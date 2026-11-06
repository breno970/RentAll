using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
[Route("Pagamentos")]
public sealed class PagamentosController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly StripeTestGateway checkout;
    private readonly StripeV2Accounts accounts;
    private readonly StripePaymentOptions options;
    private readonly ILogger<PagamentosController> log;
    public PagamentosController(ApplicationDbContext db, StripeTestGateway checkout,
        StripeV2Accounts accounts, IOptions<StripePaymentOptions> options,
        ILogger<PagamentosController> log)
    { this.db = db; this.checkout = checkout; this.accounts = accounts; this.options = options.Value; this.log = log; }

    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    private string? Origin()
    {
        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            return null;
        return uri.GetLeftPart(UriPartial.Authority);
    }

    // Read-only visual tour. Never invokes the payment gateway or saves financial data.
    [AllowAnonymous]
    [HttpGet("Demonstracao")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Demonstracao(int? pedidoId, CancellationToken ct)
    {
        if (!pedidoId.HasValue)
            return View(new CartaoDemoViewModel());
        if (User.Identity?.IsAuthenticated != true) return Challenge();

        var me = Me;
        var request = await db.PedidosAluguer.AsNoTracking()
            .Where(p => p.Id == pedidoId.Value &&
                (p.InteressadoId == me || p.AnuncianteId == me))
            .FirstOrDefaultAsync(ct);
        if (request == null || request.Pais != "PT") return NotFound();

        var itemName = await db.Anuncios.AsNoTracking().Include(a => a.item)
            .Where(a => a.id == request.AnuncioId)
            .Select(a => a.item != null ? a.item.nmitem : a.modelo)
            .FirstOrDefaultAsync(ct);

        return View(new CartaoDemoViewModel
        {
            PedidoId = request.Id,
            PedidoAssociado = true,
            Artigo = itemName ?? "Aluguer de artigo",
            Total = request.TotalEstimado,
            Comissao = request.ComissaoIndicativa,
            Pais = request.Pais
        });
    }

    [HttpGet("Conta")]
    public async Task<IActionResult> Conta(CancellationToken ct)
    {
        var connected = await db.ContasStripeLocador.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == Me, ct);
        ViewBag.Enabled = checkout.Available && Origin() != null;
        ViewBag.Ready = false;
        ViewBag.Connected = connected != null;
        if (connected != null && checkout.Available)
        {
            try { ViewBag.Ready = await accounts.CanReceiveAsync(connected.AccountId, ct); }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            { log.LogWarning(ex, "Sandbox account status query failed"); }
        }
        return View();
    }

    [HttpPost("Conectar")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Conectar(CancellationToken ct)
    {
        var origin = Origin();
        if (!checkout.Supports("PT") || origin == null) return BadRequest("Sandbox de pagamentos indisponível.");
        var connected = await db.ContasStripeLocador.FirstOrDefaultAsync(x => x.UserId == Me, ct);
        try
        {
            if (connected == null)
            {
                var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == Me, ct);
                var accountId = await accounts.CreateRecipientAsync(Me, user.Email ?? "", ct);
                connected = new ContaStripeLocador { UserId = Me, AccountId = accountId, Pais = "PT" };
                db.ContasStripeLocador.Add(connected);
                await db.SaveChangesAsync(ct);
            }
            return Redirect(await accounts.OnboardingUrlAsync(connected.AccountId, origin, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or DbUpdateException or System.Text.Json.JsonException)
        {
            log.LogError(ex, "Stripe sandbox onboarding failed");
            TempData["Error"] = "Não foi possível abrir a verificação Stripe de teste.";
            return RedirectToAction(nameof(Conta));
        }
    }

    [HttpGet("Reautorizar")]
    public async Task<IActionResult> Reautorizar(CancellationToken ct)
    {
        var current = await db.ContasStripeLocador.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == Me, ct);
        var origin = Origin();
        if (!checkout.Available || origin == null || current == null) return RedirectToAction(nameof(Conta));
        return Redirect(await accounts.OnboardingUrlAsync(current.AccountId, origin, ct));
    }

    [HttpPost("Checkout")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Checkout(int pedidoId, CancellationToken ct)
    {
        var origin = Origin();
        if (!checkout.Supports("PT") || origin == null)
        {
            TempData["Error"] = "Pagamentos indisponíveis: a sandbox ainda não está configurada.";
            return RedirectToAction("Index","Pedidos");
        }
        var order = await db.PedidosAluguer.AsNoTracking().FirstOrDefaultAsync(p =>
            p.Id == pedidoId && p.InteressadoId == Me && p.Estado == "Aceite sem pagamento",ct);
        if (order == null) return NotFound();
        if (order.Pais != "PT" || order.TotalEstimado < .5m || order.TotalEstimado > 10000m)
            return BadRequest("Pedido de pagamento não elegível.");
        var connected = await db.ContasStripeLocador.AsNoTracking().FirstOrDefaultAsync(a =>
            a.UserId == order.AnuncianteId && a.Pais == "PT",ct);
        if (connected == null)
        {
            TempData["Error"] = "O anunciante ainda não está inscrito na Stripe de teste.";
            return RedirectToAction("Index","Pedidos");
        }
        var previous = await db.PagamentosAluguer.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PedidoId == order.Id, ct);
        if (previous != null)
        {
            try
            {
                if (previous.Estado == "Checkout criado")
                {
                    var url = await checkout.ResumeCheckoutUrlAsync(previous.StripeSessionId, ct);
                    if (url != null) return Redirect(url);
                }
            }
            catch (StripeException ex) { log.LogWarning(ex, "Could not resume sandbox checkout."); }
            TempData["Error"] = "Esta tentativa já foi encerrada ou confirmada. Consulta o estado do pagamento.";
            return RedirectToAction("Index","Pedidos");
        }
        try
        {
            if (!await accounts.CanReceiveAsync(connected.AccountId,ct))
            {
                TempData["Error"] = "O anunciante ainda não concluiu a verificação Stripe.";
                return RedirectToAction("Index","Pedidos");
            }
            var cents = checked((long)decimal.Round(order.TotalEstimado * 100m, 0, MidpointRounding.AwayFromZero));
            var fee = checked((long)decimal.Round(order.ComissaoIndicativa * 100m, 0, MidpointRounding.AwayFromZero));
            if (fee < 0 || fee >= cents || cents < 50) return BadRequest("Valores inválidos.");
            var ad = await db.Anuncios.AsNoTracking().Include(a => a.item)
                .FirstOrDefaultAsync(a => a.id == order.AnuncioId,ct);
            if (ad == null || ad.iduser != order.AnuncianteId) return BadRequest("Anunciante inválido.");
            var session = await checkout.CreateCheckoutAsync(order.Id,cents,fee,
                connected.AccountId,ad.item?.nmitem ?? "Aluguer RentAll",origin,ct);
            if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
                throw new InvalidOperationException("Stripe não devolveu checkout.");
            db.PagamentosAluguer.Add(new PagamentoAluguer {
                PedidoId=order.Id, CompradorId=Me, LocadorId=order.AnuncianteId,
                StripeAccountId=connected.AccountId, StripeSessionId=session.Id,
                TotalCentimos=cents, ComissaoCentimos=fee, Moeda="eur", Estado="Checkout criado"
            });
            await db.SaveChangesAsync(ct);
            return Redirect(session.Url);
        }
        catch (Exception ex) when (ex is StripeException or InvalidOperationException or DbUpdateException)
        {
            log.LogError(ex,"Sandbox checkout failed for request {Id}",pedidoId);
            TempData["Error"]="Não foi possível iniciar o checkout de teste. Não foi confirmado qualquer pagamento.";
            return RedirectToAction("Index","Pedidos");
        }
    }
}
