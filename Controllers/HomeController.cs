using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using X.PagedList;

namespace aluguel.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext context) => _db = context;

    private IQueryable<Anuncio> AvailableAds()
    {
        return _db.Anuncios.AsNoTracking()
            .Include(a => a.item).Include(a => a.categoria).Include(a => a.tipo)
            .Where(a => a.item != null && a.item.snativo != "N");
    }

    private async Task<IActionResult> Results(MarketplaceFilters filters, string view, int? categoryOverride = null)
    {
        filters.Normalize();
        if (categoryOverride.HasValue) filters.CategoriaId = categoryOverride;
        var query = AvailableAds().Where(a => a.Pais == filters.Pais);

        if (!string.IsNullOrWhiteSpace(filters.SearchString))
        {
            var word = filters.SearchString;
            query = query.Where(a => EF.Functions.ILike(a.item!.nmitem, "%" + word + "%") ||
                                     EF.Functions.ILike(a.item!.dsitem, "%" + word + "%") ||
                                     EF.Functions.ILike(a.modelo, "%" + word + "%"));
        }
        if (filters.CategoriaId.HasValue)
            query = query.Where(a => a.categoriaid == filters.CategoriaId.Value);
        if (!string.IsNullOrWhiteSpace(filters.Estado))
            query = query.Where(a => EF.Functions.ILike(a.Estado, filters.Estado));
        if (!string.IsNullOrWhiteSpace(filters.RegionFilter))
            query = query.Where(a => EF.Functions.ILike(a.Cidade, "%" + filters.RegionFilter + "%"));
        if (!string.IsNullOrWhiteSpace(filters.Bairro))
            query = query.Where(a => EF.Functions.ILike(a.Bairro, "%" + filters.Bairro + "%"));
        if (filters.MinPrice.HasValue)
            query = query.Where(a => a.vlitem >= filters.MinPrice.Value);
        if (filters.MaxPrice.HasValue)
            query = query.Where(a => a.vlitem <= filters.MaxPrice.Value);

        query = filters.Sort switch
        {
            "preco-menor" => query.OrderBy(a => a.vlitem).ThenByDescending(a => a.id),
            "preco-maior" => query.OrderByDescending(a => a.vlitem).ThenByDescending(a => a.id),
            _ => query.OrderByDescending(a => a.id)
        };

        ViewData["Filters"] = filters;
        ViewData["CurrentFilter"] = filters.SearchString;
        ViewData["RegionFilter"] = filters.RegionFilter;
        ViewData["FilterAction"] = view;
        ViewData["Categories"] = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.nmcategoria)
            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.id.ToString(), Text = c.nmcategoria
            }).ToListAsync();

        var results = await query.ToPagedListAsync(filters.Page ?? 1, 12);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var visibleIds = results.Select(a => a.id).ToArray();
            ViewData["FavoriteIds"] = (await _db.Favoritos.AsNoTracking()
                .Where(f => f.UserId == userId && visibleIds.Contains(f.AnuncioId))
                .Select(f => f.AnuncioId).ToListAsync()).ToHashSet();
        }
        return View(view, results);
    }

    public Task<IActionResult> Index([FromQuery] MarketplaceFilters filters) => Results(filters, "Index");
    public Task<IActionResult> VideoGame([FromQuery] MarketplaceFilters filters) => Results(filters, "VideoGame", 1);
    public Task<IActionResult> AudioVisual([FromQuery] MarketplaceFilters filters) => Results(filters, "AudioVisual", 2);
    public Task<IActionResult> MusicaHobbie([FromQuery] MarketplaceFilters filters) => Results(filters, "MusicaHobbie", 3);
    public Task<IActionResult> EsporteGinastica([FromQuery] MarketplaceFilters filters) => Results(filters, "EsporteGinastica", 4);
    public Task<IActionResult> Ferramentas([FromQuery] MarketplaceFilters filters) => Results(filters, "Ferramentas", 5);
    public Task<IActionResult> Eletrodomesticos([FromQuery] MarketplaceFilters filters) => Results(filters, "Eletrodomesticos", 6);

    public IActionResult Details(int? id)
    {
        if (id is null) return NotFound();
        return RedirectToAction("Details", "Anuncios", new { id });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
