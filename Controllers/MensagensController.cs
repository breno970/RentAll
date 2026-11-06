using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public sealed class MensagensController : Controller
{
    private readonly ApplicationDbContext _db;
    public MensagensController(ApplicationDbContext db) => _db = db;
    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var me = Me;
        var messages = await _db.Mensagens.AsNoTracking()
            .Where(m => m.RemetenteId == me || m.DestinatarioId == me)
            .OrderByDescending(m => m.CriadaEm).Take(500).ToListAsync(ct);
        var requests = await _db.PedidosAluguer.AsNoTracking()
            .Where(p => p.AnuncianteId == me || p.InteressadoId == me)
            .OrderByDescending(p => p.CriadoEm).Take(150).ToListAsync(ct);
        var ids = messages.Select(m => m.AnuncioId)
            .Concat(requests.Select(p => p.AnuncioId)).Distinct().ToArray();
        var ads = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .Where(a => ids.Contains(a.id))
            .ToDictionaryAsync(a => a.id, ct);
        var threads = messages.GroupBy(m => new
        {
            m.AnuncioId,
            PeerId = m.RemetenteId == me ? m.DestinatarioId : m.RemetenteId
        }).Select(g =>
        {
            ads.TryGetValue(g.Key.AnuncioId, out var ad);
            return new ResumoConversa
            {
                AnuncioId = g.Key.AnuncioId, OutroUtilizadorId = g.Key.PeerId,
                NomeArtigo = ad?.item?.nmitem ?? "Anúncio",
                UltimaMensagem = g.Max(m => m.CriadaEm),
                UltimoTexto = g.OrderByDescending(m => m.CriadaEm).First().Texto,
                PedidoPendente = requests.Any(p => p.AnuncioId == g.Key.AnuncioId &&
                    ((p.Estado == "Pendente" && p.AnuncianteId == me) ||
                     (p.Estado == "Contraproposta" && p.PropostaPorId != me)) &&
                    p.InteressadoId == g.Key.PeerId),
                SouAnunciante = ad?.iduser == me
            };
        }).OrderByDescending(g => g.UltimaMensagem).ToList();
        // Rental requests are a valid conversation context before the first message.
        foreach (var request in requests)
        {
            var peer = request.AnuncianteId == me ? request.InteressadoId : request.AnuncianteId;
            if (threads.Any(t => t.AnuncioId == request.AnuncioId && t.OutroUtilizadorId == peer))
                continue;
            ads.TryGetValue(request.AnuncioId, out var ad);
            threads.Add(new ResumoConversa {
                AnuncioId = request.AnuncioId,
                OutroUtilizadorId = peer,
                NomeArtigo = ad?.item?.nmitem ?? "Anúncio",
                UltimaMensagem = request.CriadoEm,
                UltimoTexto = "Pedido de aluguer registado",
                PedidoPendente = (request.AnuncianteId == me && request.Estado == "Pendente") ||
                    (request.Estado == "Contraproposta" && request.PropostaPorId != me),
                SouAnunciante = request.AnuncianteId == me
            });
        }
        return View(threads.OrderByDescending(t => t.UltimaMensagem).ToList());
    }

    public async Task<IActionResult> Conversa(int anuncioId, string? peerId, CancellationToken ct)
    {
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .FirstOrDefaultAsync(a => a.id == anuncioId, ct);
        if (ad == null) return NotFound();
        var me = Me;
        bool owner = ad.iduser == me;
        var other = owner ? peerId : ad.iduser;
        if (string.IsNullOrWhiteSpace(other) || other == me) return BadRequest();
        // A rental request itself establishes a legitimate owner/interested-party
        // conversation, even if neither user has sent the first message yet.
        var hasMessage = await _db.Mensagens.AnyAsync(m => m.AnuncioId == anuncioId &&
            ((m.RemetenteId == me && m.DestinatarioId == other) ||
             (m.RemetenteId == other && m.DestinatarioId == me)), ct);
        var hasRequest = await _db.PedidosAluguer.AnyAsync(p => p.AnuncioId == anuncioId &&
            p.AnuncianteId == ad.iduser &&
            p.InteressadoId == (owner ? other : me), ct);
        if (owner && !hasMessage && !hasRequest) return NotFound();
        if (!owner && ad.item?.snativo == "N" && !hasMessage && !hasRequest)
            return NotFound();

        return View(await BuildConversationAsync(ad, owner, me, other, ct));
    }

    private async Task<ConversaViewModel> BuildConversationAsync(Anuncio ad, bool owner,
        string me, string other, CancellationToken ct)
    {
        var anuncioId = ad.id;
        var messages = await _db.Mensagens.AsNoTracking()
            .Where(m => m.AnuncioId == anuncioId &&
                ((m.RemetenteId == me && m.DestinatarioId == other) ||
                 (m.RemetenteId == other && m.DestinatarioId == me)))
            .OrderBy(m => m.CriadaEm).Take(250).ToListAsync(ct);
        var latestRequest = await _db.PedidosAluguer.AsNoTracking()
            .Where(p => p.AnuncioId == anuncioId &&
                p.InteressadoId == (owner ? other : me) && p.AnuncianteId == ad.iduser)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new { p.Id, p.Estado, p.TotalEstimado, p.Dias, p.Pais })
            .FirstOrDefaultAsync(ct);
        return new ConversaViewModel {
            AnuncioId = ad.id, NomeArtigo = ad.item?.nmitem ?? ad.modelo,
            MeuId = me, OutroUtilizadorId = other,
            SouAnunciante = owner, Mensagens = messages,
            AnuncioAtivo = ad.item?.snativo != "N",
            PrecoDia = ad.vlitem, PrazoMaximo = ad.diasAluguel > 0 ? ad.diasAluguel : 365,
            Pais = ad.Pais, PedidoId = latestRequest?.Id,
            EstadoPedido = latestRequest?.Estado, DiasPedido = latestRequest?.Dias,
            TotalPedido = latestRequest?.TotalEstimado
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("messages")]
    public async Task<IActionResult> Enviar(int anuncioId, string? peerId, string? texto, CancellationToken ct)
    {
        var me = Me;
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .FirstOrDefaultAsync(a => a.id == anuncioId, ct);
        if (ad == null) return NotFound();
        var owner = ad.iduser == me;
        var other = owner ? peerId : ad.iduser;
        if (string.IsNullOrWhiteSpace(other) || other == me) return BadRequest();
        var previous = await _db.Mensagens.AnyAsync(m => m.AnuncioId == anuncioId &&
            ((m.RemetenteId == me && m.DestinatarioId == other) ||
             (m.RemetenteId == other && m.DestinatarioId == me)), ct);
        var hasRequest = await _db.PedidosAluguer.AnyAsync(p =>
            p.AnuncioId == anuncioId && p.AnuncianteId == ad.iduser &&
            p.InteressadoId == (owner ? other : me), ct);
        if (owner && !previous && !hasRequest) return Forbid();
        if (ad.item?.snativo == "N" && !previous && !hasRequest) return NotFound();

        var body = texto?.Trim() ?? "";
        var reason = MarketplaceContactSafety.Classify(body);
        if (body.Length is < 1 or > 2000 || reason != MarketplaceContactSafety.Reason.None)
        {
            ViewBag.MessageError = reason != MarketplaceContactSafety.Reason.None
                ? MarketplaceContactSafety.UserMessage(reason)
                : "Escreve uma mensagem com até 2000 caracteres.";
            // A refused message is NOT stored or sent. Render it back safely so a
            // legitimate user can edit the text without losing the conversation.
            ViewBag.UnsentMessage = body.Length > 2000 ? body[..2000] : body;
            return View("Conversa", await BuildConversationAsync(ad, owner, me, other, ct));
        }
        _db.Mensagens.Add(new Mensagem
        {
            AnuncioId = anuncioId, RemetenteId = me, DestinatarioId = other,
            Texto = body, CriadaEm = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Conversa), new { anuncioId, peerId = other });
    }
}
