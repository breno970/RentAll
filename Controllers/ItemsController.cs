using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public sealed class ItemsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(ApplicationDbContext db, IImageStorage storage, ILogger<ItemsController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public async Task<IActionResult> Index()
        => View(await _db.Items.Where(x => x.iduser == CurrentUserId).ToListAsync());

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        return item is null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    public IActionResult GetTipos(int id)
        => Json(_db.Tipos.Where(x => x.categoriaid == id)
            .Select(x => new SelectListItem { Value = x.id.ToString(), Text = x.nmtipo }).ToList());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Create(ItemViewModel vm, CancellationToken ct)
    {
        if (vm.imagem1 is null || string.IsNullOrWhiteSpace(vm.nmitem) || string.IsNullOrWhiteSpace(vm.dsitem))
        {
            ModelState.AddModelError("", "Nome, descrição e a primeira imagem são obrigatórios.");
            return View(vm);
        }

        var uploaded = new List<string>();
        try
        {
            foreach (var image in new[] { vm.imagem1, vm.imagem2, vm.imagem3, vm.imagem4, vm.imagem5 })
            {
                if (image is not null)
                    uploaded.Add(await _storage.UploadAsync(image, CurrentUserId, ct));
                else
                    uploaded.Add("");
            }
            var item = new Item
            {
                nmitem = vm.nmitem,
                dsitem = vm.dsitem,
                dtcriacao = DateTime.UtcNow,
                snativo = vm.snativo == "N" ? "N" : "S",
                iduser = CurrentUserId,
                imagem1 = uploaded[0],
                imagem2 = string.IsNullOrEmpty(uploaded[1]) ? null : uploaded[1],
                imagem3 = string.IsNullOrEmpty(uploaded[2]) ? null : uploaded[2],
                imagem4 = string.IsNullOrEmpty(uploaded[3]) ? null : uploaded[3],
                imagem5 = string.IsNullOrEmpty(uploaded[4]) ? null : uploaded[4]
            };
            _db.Items.Add(item);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Artigo guardado. Agora podes criar o anúncio.";
            if (item.snativo != "N")
                return RedirectToAction("Create", "Anuncios", new { itemId = item.id });
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or HttpRequestException or InvalidOperationException or DbUpdateException)
        {
            _logger.LogError(ex, "Failed to create item or upload photos.");
            foreach (var key in uploaded.Where(k => !string.IsNullOrEmpty(k)))
            {
                try { await _storage.DeleteAsync(key, ct); }
                catch (Exception cleanupError) { _logger.LogWarning(cleanupError, "Could not clean up orphaned item image."); }
            }
            ModelState.AddModelError("", "Não foi possível guardar o item. Verifica o tamanho das imagens e tenta novamente.");
            return View(vm);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("id,nmitem,dsitem,snativo,imagem1,imagem2,imagem3,imagem4,imagem5,iduser,dtcriacao")] Item posted)
    {
        if (id != posted.id) return NotFound();
        var existing = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        if (existing is null) return NotFound();
        if (string.IsNullOrWhiteSpace(posted.nmitem) || string.IsNullOrWhiteSpace(posted.dsitem))
        {
            ModelState.AddModelError("", "Nome e descrição são obrigatórios.");
            return View(existing);
        }
        existing.nmitem = posted.nmitem;
        existing.dsitem = posted.dsitem;
        existing.snativo = posted.snativo == "N" ? "N" : "S";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        if (item is null) return NotFound();

        var keys = new[] { item.imagem1, item.imagem2, item.imagem3, item.imagem4, item.imagem5 };
        try
        {
            _db.Items.Remove(item);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Cannot remove item because related records still exist.");
            TempData["Error"] = "Não foi possível excluir o artigo: existem registos associados.";
            return RedirectToAction(nameof(Index));
        }
        foreach (var key in keys.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try { await _storage.DeleteAsync(key); }
            catch (Exception ex) { _logger.LogWarning(ex, "Image cleanup failed after item deletion."); }
        }
        return RedirectToAction(nameof(Index));
    }
}
