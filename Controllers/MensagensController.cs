using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
        var ids = messages.Select(m => m.AnuncioId).Distinct().ToArray();
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
                SouAnunciante = ad?.iduser == me
            };
        }).OrderByDescending(g => g.UltimaMensagem).ToList();
        return View(threads);
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
        if (owner && !await _db.Mensagens.AnyAsync(m => m.AnuncioId == anuncioId &&
            ((m.RemetenteId == me && m.DestinatarioId == other) ||
             (m.RemetenteId == other && m.DestinatarioId == me)), ct)) return NotFound();
        if (!owner && ad.item?.snativo == "N" &&
            !await _db.Mensagens.AnyAsync(m => m.AnuncioId == anuncioId &&
                ((m.RemetenteId == me && m.DestinatarioId == other) ||
                 (m.RemetenteId == other && m.DestinatarioId == me)), ct)) return NotFound();

        var messages = await _db.Mensagens.AsNoTracking()
            .Where(m => m.AnuncioId == anuncioId &&
                ((m.RemetenteId == me && m.DestinatarioId == other) ||
                 (m.RemetenteId == other && m.DestinatarioId == me)))
            .OrderBy(m => m.CriadaEm).Take(250).ToListAsync(ct);
        return View(new ConversaViewModel {
            AnuncioId = ad.id, NomeArtigo = ad.item?.nmitem ?? ad.modelo,
            MeuId = me, OutroUtilizadorId = other,
            SouAnunciante = owner, Mensagens = messages
        });
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
        if (owner && !previous) return Forbid();
        if (ad.item?.snativo == "N" && !previous) return NotFound();

        var body = texto?.Trim() ?? "";
        if (body.Length is < 1 or > 2000)
        {
            TempData["Error"] = "Escreve uma mensagem com até 2000 caracteres.";
            return RedirectToAction(nameof(Conversa), new { anuncioId, peerId = other });
        }
        // Do not accept links, email addresses or telephone numbers, which can leak private
        // information and bypass any future buyer/seller protections.
        if (Regex.IsMatch(body, @"\bhttps?://|\bwww\.|[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", RegexOptions.IgnoreCase)
            || Regex.IsMatch(body, @"(?<!\d)(?:\+?\d[\s().-]?){9,15}(?!\d)"))
        {
            TempData["Error"] = "Por segurança, não incluas contactos, números de telefone ou links. Combina tudo pela RentAll.";
            return RedirectToAction(nameof(Conversa), new { anuncioId, peerId = other });
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
