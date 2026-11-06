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

    public async Task<IActionResult> Index(string? vista, CancellationToken ct)
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
        // Dashboard filters work server-side and never expose other accounts.
        vista = vista switch {
            "recebidos" => "recebidos",
            "enviados" => "enviados",
            "pendentes" => "pendentes",
            _ => "todos"
        };
        ViewBag.SelectedView = vista;
        ViewBag.TotalRequests = requests.Count;
        ViewBag.ReceivedCount = requests.Count(p => p.AnuncianteId == me);
        ViewBag.SentCount = requests.Count(p => p.InteressadoId == me);
        ViewBag.PendingToAnswer = requests.Count(p => p.AnuncianteId == me && p.Estado == "Pendente");
        var visible = vista switch {
            "recebidos" => requests.Where(p => p.AnuncianteId == me).ToList(),
            "enviados" => requests.Where(p => p.InteressadoId == me).ToList(),
            "pendentes" => requests.Where(p => p.AnuncianteId == me && p.Estado == "Pendente").ToList(),
            _ => requests
        };
        ViewBag.AdNames = adNames;
        ViewBag.Me = me;
        ViewBag.StripeSandboxReady = _stripe.Available;
        var requestIds = requests.Select(r => r.Id).ToArray();
        ViewBag.PaymentStates = await _db.PagamentosAluguer.AsNoTracking()
            .Where(p => requestIds.Contains(p.PedidoId))
            .ToDictionaryAsync(p => p.PedidoId, p => p.Estado, ct);
        return View(visible);
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
            p.InteressadoId == me && (p.Estado == "Pendente" || p.Estado == "Contraproposta"), ct))
        {
            TempData["Error"] = "Já tens uma negociação em curso para este artigo.";
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
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Este pedido já foi alterado por outro participante.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = aceitar
            ? "Pedido aceite. Combina os detalhes nas mensagens. O pagamento na RentAll ainda não está disponível."
            : "Pedido recusado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Propor(int id, int dias, decimal precoDia, CancellationToken ct)
    {
        var me = Me;
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p =>
            p.Id == id && (p.AnuncianteId == me || p.InteressadoId == me), ct);
        if (request == null) return NotFound();
        if (request.Estado != "Pendente")
        {
            TempData["Error"] = "Já existe uma resposta ou contraproposta para este pedido.";
            return RedirectToAction(nameof(Index));
        }
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .FirstOrDefaultAsync(a => a.id == request.AnuncioId, ct);
        if (ad == null || ad.item?.snativo == "N")
        {
            TempData["Error"] = "Este anúncio já não está disponível.";
            return RedirectToAction(nameof(Index));
        }
        precoDia = decimal.Round(precoDia, 2, MidpointRounding.AwayFromZero);
        if (dias < 1 || dias > 365 ||
            (ad.diasAluguel > 0 && dias > ad.diasAluguel) ||
            precoDia < 0.50m || precoDia > 10000m || precoDia * dias > 10000m)
        {
            TempData["Error"] = "Indica entre 1 e 365 dias e um valor por dia válido. O total não pode exceder 10 000 na moeda do anúncio.";
            return RedirectToAction(nameof(Index));
        }
        request.PropostaDias = dias;
        request.PropostaPrecoDia = precoDia;
        request.PropostaPorId = me;
        request.PropostaEm = DateTime.UtcNow;
        request.Estado = "Contraproposta";
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "O pedido foi atualizado ao mesmo tempo. Consulta o estado mais recente.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Contraproposta registada. A outra pessoa terá de aceitar ou recusar antes de alterar as condições.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> DecidirProposta(int id, bool aceitar, CancellationToken ct)
    {
        var me = Me;
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p =>
            p.Id == id && (p.AnuncianteId == me || p.InteressadoId == me), ct);
        if (request == null) return NotFound();
        if (request.Estado != "Contraproposta" || request.PropostaPorId == me ||
            !request.PropostaDias.HasValue || !request.PropostaPrecoDia.HasValue)
        {
            TempData["Error"] = "Não existe uma contraproposta tua para decidir neste pedido.";
            return RedirectToAction(nameof(Index));
        }
        if (aceitar)
        {
            var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
                .FirstOrDefaultAsync(a => a.id == request.AnuncioId, ct);
            if (ad == null || ad.item?.snativo == "N")
            {
                TempData["Error"] = "Este anúncio já não está disponível.";
                return RedirectToAction(nameof(Index));
            }
            if (ad.diasAluguel > 0 && request.PropostaDias.Value > ad.diasAluguel)
            {
                TempData["Error"] = "O período proposto excede o limite atual do anúncio.";
                return RedirectToAction(nameof(Index));
            }
            request.Dias = request.PropostaDias.Value;
            request.PrecoDia = request.PropostaPrecoDia.Value;
            request.TotalEstimado = decimal.Round(request.PrecoDia * request.Dias, 2, MidpointRounding.AwayFromZero);
            request.ComissaoIndicativa = decimal.Round(request.TotalEstimado * 0.10m, 2, MidpointRounding.AwayFromZero);
            request.Estado = "Aceite sem pagamento";
            request.RespondidoEm = DateTime.UtcNow;
        }
        else request.Estado = "Pendente";
        request.PropostaDias = null;
        request.PropostaPrecoDia = null;
        request.PropostaPorId = null;
        request.PropostaEm = null;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "O pedido foi atualizado. Confirma a situação atual antes de responder.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = aceitar
            ? "Contraproposta aceite. As condições foram atualizadas, mas ainda não existe pagamento."
            : "Contraproposta recusada. O pedido original continua pendente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> RetirarProposta(int id, CancellationToken ct)
    {
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p =>
            p.Id == id && p.PropostaPorId == Me && p.Estado == "Contraproposta", ct);
        if (request == null) return NotFound();
        request.Estado = "Pendente";
        request.PropostaDias = null;
        request.PropostaPrecoDia = null;
        request.PropostaPorId = null;
        request.PropostaEm = null;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Este pedido já mudou de estado.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Contraproposta retirada. O pedido original mantém-se pendente.";
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
        if (request.Estado != "Pendente" && request.Estado != "Contraproposta")
            return RedirectToAction(nameof(Index));
        request.Estado = "Cancelado";
        request.RespondidoEm = DateTime.UtcNow;
        request.PropostaDias = null;
        request.PropostaPrecoDia = null;
        request.PropostaPorId = null;
        request.PropostaEm = null;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "O estado do pedido foi atualizado antes do cancelamento.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Pedido cancelado.";
        return RedirectToAction(nameof(Index));
    }
}
