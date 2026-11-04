using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public sealed class PedidosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly aluguel.Services.StripeTestGateway _stripe;
    public PedidosController(ApplicationDbContext db, aluguel.Services.StripeTestGateway stripe) { _db = db; _stripe = stripe; }
    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var me = Me;
        var requests = await _db.PedidosAluguer.AsNoTracking()
            .Where(p => p.InteressadoId == me || p.AnuncianteId == me)
            .OrderByDescending(p => p.CriadoEm).Take(150).ToListAsync(ct);
        var adIds = requests.Select(p => p.AnuncioId).Distinct().ToArray();
        var adNames = await _db.Anuncios.AsNoTracking()
            .Include(a => a.item).Where(a => adIds.Contains(a.id))
            .Select(a => new { a.id, Nome = a.item != null ? a.item.nmitem : a.modelo })
            .ToDictionaryAsync(a => a.id, a => a.Nome, ct);
        ViewBag.AdNames = adNames;
        ViewBag.Me = me;
        ViewBag.StripeSandboxReady = _stripe.Available;
        ViewBag.PaymentStates = await _db.PagamentosAluguer.AsNoTracking()
            .Where(p => requests.Select(r => r.Id).Contains(p.PedidoId))
            .ToDictionaryAsync(p => p.PedidoId, p => p.Estado, ct);
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Criar(int anuncioId, int dias, CancellationToken ct)
    {
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .FirstOrDefaultAsync(a => a.id == anuncioId, ct);
        if (ad == null || ad.item == null || ad.item.snativo == "N")
            return NotFound();
        var me = Me;
        if (me == ad.iduser) return BadRequest("Não podes pedir o aluguer do teu próprio artigo.");
        if (dias < 1 || dias > 365 || (ad.diasAluguel > 0 && dias > ad.diasAluguel))
        {
            TempData["Error"] = "Seleciona um período válido para este anúncio.";
            return RedirectToAction("Details", "Anuncios", new { id = anuncioId });
        }
        // A configured daily price is needed for a meaningful fee estimate.
        // Price-to-be-agreed listings stay in messaging, without invented totals.
        if (ad.vlitem <= 0 || ad.vlitem > 10000000)
        {
            TempData["Error"] = "Este artigo tem preço a combinar. Fala com o anunciante nas mensagens.";
            return RedirectToAction("Details", "Anuncios", new { id = anuncioId });
        }
        if (await _db.PedidosAluguer.AnyAsync(p => p.AnuncioId == anuncioId &&
            p.InteressadoId == me && p.Estado == "Pendente", ct))
        {
            TempData["Error"] = "Já tens um pedido pendente para este artigo.";
            return RedirectToAction(nameof(Index));
        }
        decimal daily = decimal.Round((decimal)ad.vlitem, 2, MidpointRounding.AwayFromZero);
        decimal gross = decimal.Round(daily * dias, 2, MidpointRounding.AwayFromZero);
        decimal indicativeFee = decimal.Round(gross * 0.10m, 2, MidpointRounding.AwayFromZero);
        _db.PedidosAluguer.Add(new PedidoAluguer
        {
            AnuncioId = anuncioId, InteressadoId = me, AnuncianteId = ad.iduser,
            Dias = dias, PrecoDia = daily, Pais = ad.Pais, TotalEstimado = gross,
            ComissaoIndicativa = indicativeFee, Estado = "Pendente", CriadoEm = DateTime.UtcNow
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index also prevents simultaneous duplicate requests.
            TempData["Error"] = "Não foi possível registar este pedido. Verifica se já tens um pedido pendente.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Pedido enviado. O anunciante pode aceitá-lo ou recusá-lo. Ainda não foi feito nenhum pagamento.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Responder(int id, bool aceitar, CancellationToken ct)
    {
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p => p.Id == id &&
            p.AnuncianteId == Me, ct);
        if (request == null) return NotFound();
        if (request.Estado != "Pendente")
        {
            TempData["Error"] = "Este pedido já foi respondido.";
            return RedirectToAction(nameof(Index));
        }
        request.Estado = aceitar ? "Aceite sem pagamento" : "Recusado";
        request.RespondidoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = aceitar
            ? "Pedido aceite. Combina os detalhes nas mensagens. O pagamento na RentAll ainda não está disponível."
            : "Pedido recusado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p => p.Id == id &&
            p.InteressadoId == Me, ct);
        if (request == null) return NotFound();
        if (request.Estado != "Pendente") return RedirectToAction(nameof(Index));
        request.Estado = "Cancelado";
        request.RespondidoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Pedido cancelado.";
        return RedirectToAction(nameof(Index));
    }
}
