using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public sealed class DenunciasController : Controller
{
    private static readonly HashSet<string> Reasons = new(StringComparer.Ordinal) {
        "Artigo suspeito", "Informação falsa", "Pedido de dados pessoais",
        "Possível fraude", "Conteúdo proibido", "Outro"
    };
    private readonly ApplicationDbContext _db;
    public DenunciasController(ApplicationDbContext db) => _db = db;
    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Criar(int anuncioId, string motivo, string? detalhes, CancellationToken ct)
    {
        var user = Me;
        if (!Reasons.Contains(motivo))
            return BadRequest("Seleciona um motivo de denúncia válido.");
        if (detalhes?.Length > 700)
            return BadRequest("A descrição da denúncia não pode ultrapassar 700 caracteres.");
        var ad = await _db.Anuncios.AsNoTracking()
            .FirstOrDefaultAsync(a => a.id == anuncioId, ct);
        if (ad == null) return NotFound();
        if (ad.iduser == user) return BadRequest("Não podes denunciar o teu próprio anúncio.");
        if (await _db.DenunciasAnuncio.AnyAsync(d =>
            d.AnuncioId == anuncioId && d.AutorId == user, ct))
        {
            TempData["Success"] = "Já recebemos uma denúncia tua sobre este anúncio.";
            return RedirectToAction("Details", "Anuncios", new { id = anuncioId });
        }
        _db.DenunciasAnuncio.Add(new DenunciaAnuncio
        {
            AnuncioId = anuncioId, AutorId = user, Motivo = motivo,
            Detalhes = detalhes?.Trim(), CriadaEm = DateTime.UtcNow
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Não foi possível registar outra denúncia para este anúncio.";
            return RedirectToAction("Details", "Anuncios", new { id = anuncioId });
        }
        TempData["Success"] = "Denúncia recebida. A equipa responsável poderá analisá-la. Não significa que o anúncio tenha sido removido.";
        return RedirectToAction("Details", "Anuncios", new { id = anuncioId });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _db.DenunciasAnuncio.AsNoTracking()
            .OrderBy(d => d.Estado).ThenByDescending(d => d.CriadaEm)
            .Take(200).ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rever(int id, bool resolvida, CancellationToken ct)
    {
        var report = await _db.DenunciasAnuncio.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (report == null) return NotFound();
        report.Estado = resolvida ? "Revista" : "Pendente";
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }
}
