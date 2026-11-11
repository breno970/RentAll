using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using X.PagedList;

namespace aluguel.Controllers;

[Authorize]
public sealed class PedidosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly aluguel.Services.StripeTestGateway _stripe;
    public PedidosController(ApplicationDbContext db, aluguel.Services.StripeTestGateway stripe) { _db = db; _stripe = stripe; }
    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public async Task<IActionResult> Index(string? vista, int? pagina, int? pedido, CancellationToken ct)
    {
        var me = Me;
        var all = _db.PedidosAluguer.AsNoTracking()
            .Where(p => p.InteressadoId == me || p.AnuncianteId == me);
        var pending = all.Where(p =>
            (p.AnuncianteId == me && p.Estado == "Pendente") ||
            (p.Estado == "Contraproposta" && p.PropostaPorId != me));
        vista = vista is "recebidos" or "enviados" or "pendentes" ? vista : "todos";
        var filtered = vista switch
        {
            "recebidos" => all.Where(p => p.AnuncianteId == me),
            "enviados" => all.Where(p => p.InteressadoId == me),
            "pendentes" => pending,
            _ => all
        };
        if (pedido.HasValue)
        {
            filtered = all.Where(p => p.Id == pedido.Value);
            if (!await filtered.AnyAsync(ct)) return NotFound();
            vista = "todos";
            pagina = 1;
        }
        var requests = await filtered.OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
            .ToPagedListAsync(Math.Clamp(pagina ?? 1, 1, int.MaxValue / 20), 20);
        if (requests.PageNumber > Math.Max(1, requests.PageCount))
            return RedirectToAction(nameof(Index), new { vista, pagina = Math.Max(1, requests.PageCount) });

        ViewBag.SelectedView = vista;
        ViewBag.FocusedRequest = pedido;
        ViewBag.ReceivedCount = await all.CountAsync(p => p.AnuncianteId == me, ct);
        ViewBag.SentCount = await all.CountAsync(p => p.InteressadoId == me, ct);
        ViewBag.TotalRequests = await all.CountAsync(ct);
        ViewBag.PendingToAnswer = await pending.CountAsync(ct);
        var adIds = requests.Select(p => p.AnuncioId).Distinct().ToArray();
        ViewBag.AdNames = await _db.Anuncios.AsNoTracking()
            .Where(a => adIds.Contains(a.id))
            .Select(a => new { a.id, Nome = a.item != null ? a.item.nmitem : a.modelo })
            .ToDictionaryAsync(a => a.id, a => a.Nome, ct);
        ViewBag.Me = me;
        ViewBag.StripeSandboxReady = _stripe.Available;
        var requestIds = requests.Select(r => r.Id).ToArray();
        var acceptedIds = requests.Where(r => r.Estado == "Aceite sem pagamento").Select(r => r.Id).ToArray();
        ViewBag.JourneyStates = await _db.PercursosAluguerDemo.AsNoTracking()
            .Where(journey => acceptedIds.Contains(journey.PedidoId))
            .ToDictionaryAsync(journey => journey.PedidoId, journey => journey.Estado, ct);
        ViewBag.PaymentStates = await _db.PagamentosAluguer.AsNoTracking()
            .Where(p => requestIds.Contains(p.PedidoId))
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
        if (ad == null || ad.Pais != "PT" || ad.item == null || ad.item.snativo == "N")
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
        if (!float.IsFinite(ad.vlitem) || ad.vlitem < 0.005f || ad.vlitem > 10000000)
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
        var request = new PedidoAluguer
        {
            AnuncioId = anuncioId, InteressadoId = me, AnuncianteId = ad.iduser,
            Dias = dias, PrecoDia = daily, Pais = ad.Pais, TotalEstimado = gross,
            ComissaoIndicativa = indicativeFee, Estado = "Pendente", CriadoEm = DateTime.UtcNow
        };
        _db.PedidosAluguer.Add(request);
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
        return RedirectToAction(nameof(Index), new { pedido = request.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Responder(int id, bool? aceitar, CancellationToken ct)
    {
        if (!aceitar.HasValue || !ModelState.IsValid) return BadRequest();
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p => p.Id == id &&
            p.AnuncianteId == Me, ct);
        if (request == null) return NotFound();
        if (request.Estado != "Pendente")
        {
            TempData["Error"] = "Este pedido já foi respondido.";
            return RedirectToAction(nameof(Index));
        }
        if (aceitar.Value && !await CanAcceptAsync(request, request.Dias, ct))
            return RedirectToAction(nameof(Index));

        request.Estado = aceitar.Value ? "Aceite sem pagamento" : "Recusado";
        request.RespondidoEm = DateTime.UtcNow;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Este pedido já foi alterado por outro participante.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = aceitar.Value
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
        if (ad is null || ad.Pais != "PT" || ad.item is null || ad.item.snativo == "N")
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
    public async Task<IActionResult> DecidirProposta(int id, bool? aceitar, long versaoProposta, CancellationToken ct)
    {
        if (!aceitar.HasValue || !ModelState.IsValid) return BadRequest();
        var me = Me;
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p =>
            p.Id == id && (p.AnuncianteId == me || p.InteressadoId == me), ct);
        if (request == null) return NotFound();
        if (request.Estado != "Contraproposta" || request.PropostaPorId == me ||
            !request.PropostaDias.HasValue || !request.PropostaPrecoDia.HasValue)
        {
            TempData["Error"] = "Não tens uma contraproposta para decidir neste pedido.";
            return RedirectToAction(nameof(Index));
        }
        if (versaoProposta != (request.PropostaEm?.Ticks ?? 0))
        {
            TempData["Error"] = "A contraproposta mudou. Revê as condições atuais antes de responder.";
            return RedirectToAction(nameof(Index), new { pedido = id });
        }
        if (aceitar.Value)
        {
            if (!await CanAcceptAsync(request, request.PropostaDias.Value, ct))
                return RedirectToAction(nameof(Index));
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
        TempData["Success"] = aceitar.Value
            ? "Contraproposta aceite. As condições foram atualizadas, mas ainda não existe pagamento."
            : "Contraproposta recusada. O pedido original continua pendente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanAcceptAsync(PedidoAluguer request, int days, CancellationToken ct)
    {
        var listing = await _db.Anuncios.AsNoTracking()
            .Where(ad => ad.id == request.AnuncioId && ad.iduser == request.AnuncianteId &&
                         ad.Pais == "PT" && ad.item != null && ad.item.snativo != "N")
            .Select(ad => new { MaxDays = ad.diasAluguel })
            .SingleOrDefaultAsync(ct);
        if (listing is null)
        {
            TempData["Error"] = "Este anúncio já não está disponível. Podes recusar o pedido.";
            return false;
        }

        if (days < 1 || days > 365 || (listing.MaxDays > 0 && days > listing.MaxDays))
        {
            TempData["Error"] = "O período excede o limite atual do anúncio. Podes enviar uma contraproposta.";
            return false;
        }

        return true;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> RetirarProposta(int id, long versaoProposta, CancellationToken ct)
    {
        var request = await _db.PedidosAluguer.FirstOrDefaultAsync(p =>
            p.Id == id && p.PropostaPorId == Me && p.Estado == "Contraproposta", ct);
        if (request == null) return NotFound();
        if (versaoProposta != (request.PropostaEm?.Ticks ?? 0))
        {
            TempData["Error"] = "A contraproposta mudou. Revê as condições atuais antes de a retirar.";
            return RedirectToAction(nameof(Index), new { pedido = id });
        }
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
