using aluguel.Areas.Identity.Data;
using aluguel.Models;
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _db.Anuncios.AsNoTracking()
            .Include(a => a.item).Include(a => a.categoria).Include(a => a.tipo)
            .Where(a => a.item != null && a.item.snativo != "N" && (userId == null || a.iduser != userId));
    }

    public async Task<IActionResult> Index(string? searchString, string? RegionFilter, int? page)
    {
        var query = AvailableAds();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            query = query.Where(a => EF.Functions.ILike(a.item!.nmitem, "%" + term + "%") ||
                                     EF.Functions.ILike(a.modelo, "%" + term + "%"));
        }
        if (!string.IsNullOrWhiteSpace(RegionFilter))
        {
            var region = RegionFilter.Trim();
            query = query.Where(a => EF.Functions.ILike(a.Cidade, "%" + region + "%"));
        }
        ViewData["CurrentFilter"] = searchString;
        ViewData["RegionFilter"] = RegionFilter;
        return View(await query.OrderByDescending(a => a.id).ToPagedListAsync(Math.Max(1, page ?? 1), 9));
    }

    private async Task<IActionResult> Category(string view, int id, string? searchString, int? page)
    {
        var query = AvailableAds().Where(a => a.categoriaid == id);
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            query = query.Where(a => EF.Functions.ILike(a.item!.nmitem, "%" + term + "%") ||
                                     EF.Functions.ILike(a.modelo, "%" + term + "%"));
        }
        ViewData["CurrentFilter"] = searchString;
        return View(view, await query.OrderByDescending(a => a.id).ToPagedListAsync(Math.Max(1, page ?? 1), 12));
    }

    public Task<IActionResult> VideoGame(string? searchString, int? page) => Category("VideoGame", 1, searchString, page);
    public Task<IActionResult> AudioVisual(string? searchString, int? page) => Category("AudioVisual", 2, searchString, page);
    public Task<IActionResult> MusicaHobbie(string? searchString, int? page) => Category("MusicaHobbie", 3, searchString, page);
    public Task<IActionResult> EsporteGinastica(string? searchString, int? page) => Category("EsporteGinastica", 4, searchString, page);
    public Task<IActionResult> Ferramentas(string? searchString, int? page) => Category("Ferramentas", 5, searchString, page);
    public Task<IActionResult> Eletrodomesticos(string? searchString, int? page) => Category("Eletrodomesticos", 6, searchString, page);

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var anuncio = await AvailableAds().FirstOrDefaultAsync(a => a.id == id);
        return anuncio is null ? NotFound() : View(anuncio);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
