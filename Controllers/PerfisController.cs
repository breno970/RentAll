using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aluguel.Controllers;

/// <summary>Only public listings and an opted-in listing nickname are exposed.</summary>
public sealed class PerfisController : Controller
{
    private readonly ApplicationDbContext _db;
    public PerfisController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Details(string? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var owner = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.CreatedAt })
            .FirstOrDefaultAsync(ct);
        if (owner is null) return NotFound();

        var listings = await _db.Anuncios.AsNoTracking()
            .Include(a => a.item).Include(a => a.categoria).Include(a => a.tipo)
            .Where(a => a.iduser == id && a.Pais == "PT" && a.item != null && a.item.snativo != "N")
            .OrderByDescending(a => a.id)
            .Take(60).ToListAsync(ct);

        // A non-selling account isn't publicly discoverable.
        if (listings.Count == 0) return NotFound();
        return View(new aluguel.ViewModel.PublicSellerProfile
        {
            PublicName = listings.Select(a => a.NomeFantasia)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Anunciante RentAll",
            RegisteredAt = owner.CreatedAt,
            FirstPublication = listings.Min(a => a.item!.dtcriacao),
            Listings = listings
        });
    }
}
