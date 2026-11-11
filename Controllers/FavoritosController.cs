using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public class FavoritosController : Controller
{
    private readonly ApplicationDbContext _db;

    public FavoritosController(ApplicationDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _db.Favoritos.AsNoTracking()
            .Where(f => f.UserId == UserId && f.Anuncio != null && f.Anuncio.Pais == "PT" && f.Anuncio.item != null && f.Anuncio.item.snativo != "N")
            .OrderByDescending(f => f.CriadoEm)
            .Include(f => f.Anuncio).ThenInclude(a => a!.item)
            .Include(f => f.Anuncio).ThenInclude(a => a!.categoria)
            .Include(f => f.Anuncio).ThenInclude(a => a!.tipo)
            .ToListAsync();

        ViewData["FavoriteIds"] = items.Select(f => f.AnuncioId).ToHashSet();
        return View(items.Where(f => f.Anuncio != null).Select(f => f.Anuncio!).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int anuncioId, string? returnUrl)
    {
        var existing = await _db.Favoritos
            .FirstOrDefaultAsync(f => f.UserId == UserId && f.AnuncioId == anuncioId);
        if (existing == null)
        {
            var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
                .FirstOrDefaultAsync(a => a.id == anuncioId);
            if (ad == null || ad.Pais != "PT" || ad.item == null || ad.item.snativo == "N")
                return NotFound();
            _db.Favoritos.Add(new Favorito { UserId = UserId, AnuncioId = anuncioId });
        }
        else
        {
            _db.Favoritos.Remove(existing);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Repeated clicks must not cause an application error.
            TempData["Error"] = "Não foi possível atualizar os favoritos.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
