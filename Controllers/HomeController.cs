using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using aluguel.Models;
using aluguel.ViewModel;
using aluguel.Areas.Identity.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

namespace aluguel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext Context)
        {
            _context = Context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId);

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 6;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> VideoGame(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(1));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> AudioVisual(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(2));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> MusicaHobbie(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(3));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> EsporteGinastica(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(4));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> Ferramentas(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(5));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> Eletrodomesticos(string searchString, int? page)
        {
            var userId = User.Identity.IsAuthenticated
                ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value
                : null;

            var home = _context.Anuncios
                .Include(a => a.item)
                .Where(a => userId == null || a.iduser != userId)
                .Include(a => a.categoria)
                .Where(a => a.categoriaid.Equals(6));

            if (!string.IsNullOrEmpty(searchString))
            {
                home = home.Where(a => a.item.nmitem.Contains(searchString));
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var model = await home.ToPagedListAsync(pageNumber, pageSize);
            ViewData["CurrentFilter"] = searchString;

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Anuncios == null)
            {
                return NotFound();
            }

            var anuncio = await _context.Anuncios
                .Include(a => a.item)
                .FirstOrDefaultAsync(m => m.id == id);
            if (anuncio == null)
            {
                return NotFound();
            }

            return View(anuncio);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}