using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    private static List<string> Images(Item item)
        => new[] { item.imagem1, item.imagem2, item.imagem3, item.imagem4, item.imagem5 }
           .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

    private static void ApplyImages(Item item, IReadOnlyList<string> keys)
    {
        item.imagem1 = keys.ElementAtOrDefault(0)!;
        item.imagem2 = keys.ElementAtOrDefault(1);
        item.imagem3 = keys.ElementAtOrDefault(2);
        item.imagem4 = keys.ElementAtOrDefault(3);
        item.imagem5 = keys.ElementAtOrDefault(4);
    }

    private static List<string> ParseOrder(string? raw, IReadOnlyCollection<string> existing, int fileCount)
    {
        var tokens = (raw ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != existing.Count + fileCount || tokens.Length is < 1 or > 5)
            throw new InvalidDataException("Seleciona entre 1 e 5 fotografias.");
        var allowed = new HashSet<string>(existing.Select(k => "e:" + k), StringComparer.Ordinal);
        for (var i = 0; i < fileCount; i++) allowed.Add("n:" + i);
        if (tokens.Distinct(StringComparer.Ordinal).Count() != tokens.Length || tokens.Any(t => !allowed.Contains(t)))
            throw new InvalidDataException("A ordem das fotografias é inválida. Atualiza a página e tenta novamente.");
        return tokens.ToList();
    }

    private async Task CleanupAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        foreach (var key in keys)
        {
            try { await _storage.DeleteAsync(key, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível limpar imagem órfã."); }
        }
    }

    public async Task<IActionResult> Index()
        => View(await _db.Items.Where(x => x.iduser == CurrentUserId).ToListAsync());

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId);
        return item is null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(35_000_000)]
    public async Task<IActionResult> Create(string nmitem, string dsitem, string snativo, List<IFormFile>? photos, string? photoOrder, CancellationToken ct)
    {
        photos ??= new List<IFormFile>();
        if (string.IsNullOrWhiteSpace(nmitem) || string.IsNullOrWhiteSpace(dsitem))
            ModelState.AddModelError("", "Preenche o nome e a descrição do artigo.");
        if (!ModelState.IsValid) return View();
        var uploaded = new List<string>();
        try
        {
            var order = ParseOrder(photoOrder, Array.Empty<string>(), photos.Count);
            foreach (var photo in photos) uploaded.Add(await _storage.UploadAsync(photo, CurrentUserId, ct));
            var arranged = order.Select(token => uploaded[int.Parse(token[2..])]).ToList();
            var item = new Item
            {
                nmitem = nmitem.Trim(), dsitem = dsitem.Trim(),
                dtcriacao = DateTime.UtcNow,
                snativo = snativo == "N" ? "N" : "S",
                iduser = CurrentUserId
            };
            ApplyImages(item, arranged);
            _db.Items.Add(item);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Artigo guardado. Agora podes criar o anúncio.";
            if (item.snativo != "N")
                return RedirectToAction("Create", "Anuncios", new { itemId = item.id });
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or HttpRequestException or InvalidOperationException or DbUpdateException)
        {
            _logger.LogError(ex, "Failed to create item or upload photos");
            await CleanupAsync(uploaded);
            ModelState.AddModelError("", ex is InvalidDataException ? ex.Message :
                ex is ImageStorageException storageError ? storageError.UserMessage :
                "Não foi possível guardar o artigo. O serviço de imagens ou a base de dados não está disponível.");
            return View();
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
    [RequestSizeLimit(35_000_000)]
    public async Task<IActionResult> Edit(int id, string nmitem, string dsitem, string snativo, List<IFormFile>? photos, string? photoOrder, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.id == id && x.iduser == CurrentUserId, ct);
        if (item is null) return NotFound();
        photos ??= new List<IFormFile>();
        var current = Images(item);
        if (string.IsNullOrWhiteSpace(nmitem) || string.IsNullOrWhiteSpace(dsitem))
        {
            ModelState.AddModelError("", "Nome e descrição são obrigatórios.");
            return View(item);
        }
        var uploaded = new List<string>();
        try
        {
            // Only the image keys that belong to this item may be retained.
            var tokens = (photoOrder ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var retained = current.Where(k => tokens.Contains("e:" + k, StringComparer.Ordinal)).ToList();
            var order = ParseOrder(photoOrder, retained, photos.Count);
            foreach (var file in photos) uploaded.Add(await _storage.UploadAsync(file, CurrentUserId, ct));
            var arranged = order.Select(token => token.StartsWith("e:", StringComparison.Ordinal)
                ? token[2..] : uploaded[int.Parse(token[2..])]).ToList();
            item.nmitem = nmitem.Trim();
            item.dsitem = dsitem.Trim();
            item.snativo = snativo == "N" ? "N" : "S";
            ApplyImages(item, arranged);
            await _db.SaveChangesAsync(ct);
            await CleanupAsync(current.Except(retained));
            TempData["Success"] = "Artigo atualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or HttpRequestException or InvalidOperationException or DbUpdateException)
        {
            _logger.LogError(ex, "Failed to update item {ItemId}", id);
            await CleanupAsync(uploaded);
            ModelState.AddModelError("", ex is InvalidDataException ? ex.Message :
                ex is ImageStorageException storageError ? storageError.UserMessage :
                "Não foi possível guardar as alterações. Verifica o serviço de fotografias.");
            // A failed database save should not display unsaved image keys.
            await _db.Entry(item).ReloadAsync(ct);
            return View(item);
        }
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
        var keys = Images(item);
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
        await CleanupAsync(keys);
        return RedirectToAction(nameof(Index));
    }
}
