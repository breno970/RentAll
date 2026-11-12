using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

[Authorize]
public class AnunciosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AnunciosController> _logger;
    public AnunciosController(ApplicationDbContext db, ILogger<AnunciosController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    private async Task PrepareCreateOptions(int? selectedItemId = null)
    {
        ViewBag.Categoria = await _db.Categorias.AsNoTracking().OrderBy(x => x.nmcategoria)
            .Select(x => new SelectListItem { Value = x.id.ToString(), Text = x.nmcategoria }).ToListAsync();
        var items = await _db.Items.AsNoTracking()
            .Where(x => x.iduser == UserId && x.snativo != "N").OrderBy(x => x.nmitem).ToListAsync();
        ViewBag.itemid = new SelectList(items, "id", "nmitem", selectedItemId);
    }

    public async Task<IActionResult> Index()
    {
        var ads = await _db.Anuncios.AsNoTracking().Where(a => a.iduser == UserId)
            .Include(a => a.item).Include(a => a.categoria).Include(a => a.tipo)
            .OrderByDescending(a => a.id).ToListAsync();
        return View(ads);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDemoListings()
    {
        var samples = aluguel.ViewModel.DemoCatalogItem.All;
        var existing = (await _db.Anuncios.AsNoTracking()
            .Where(a => a.iduser == UserId && a.modelo.StartsWith("[DEMO] ") && a.item != null)
            .Select(a => a.item!.nmitem).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var missing = samples.Where(sample => !existing.Contains("[DEMO] " + sample.Name)).ToArray();
        if (missing.Length == 0)
        {
            TempData["Success"] = "Já tens todos os artigos de exemplo.";
            return RedirectToAction(nameof(Index));
        }

        var categories = await _db.Categorias.AsNoTracking()
            .Where(c => c.id >= 1 && c.id <= 6).Select(c => c.id).ToListAsync();
        var types = await _db.Tipos.AsNoTracking().OrderBy(t => t.id).ToListAsync();
        var typeByCategory = types.GroupBy(t => t.categoriaid)
            .ToDictionary(group => group.Key, group => group.First().id);
        var available = missing.Where(sample => categories.Contains(sample.CategoryId) &&
            typeByCategory.ContainsKey(sample.CategoryId)).ToArray();

        if (available.Length == 0)
        {
            TempData["Error"] = "Não existem categorias e tipos configurados para gerar anúncios de exemplo.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var sample in available)
        {
            var item = new Item
            {
                nmitem = "[DEMO] " + sample.Name,
                dsitem = "Artigo de exemplo da RentAll. Este anúncio pode ser removido no painel da conta que o criou.",
                dtcriacao = DateTime.UtcNow,
                snativo = "S",
                imagem1 = "ra.png",
                iduser = UserId
            };
            _db.Items.Add(item);
            _db.Anuncios.Add(new Anuncio
            {
                item = item,
                vlitem = sample.DailyPrice,
                diasAluguel = 7,
                modelo = "[DEMO] " + sample.Description,
                CEP = "",
                Rua = "",
                Bairro = "Centro",
                Cidade = sample.City,
                Estado = sample.City,
                contato = "",
                NomeFantasia = "RentAll · Artigos de exemplo",
                Pais = "PT",
                iduser = UserId,
                categoriaid = sample.CategoryId,
                tipoid = typeByCategory[sample.CategoryId]
            });
        }

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Foram criados {available.Length} anúncios fictícios, distribuídos pelas categorias disponíveis. Podes apagá-los nesta área.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not create demo rental listings.");
            TempData["Error"] = "Não foi possível criar os anúncios de exemplo. Tente novamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDemoListings()
    {
        var demoAds = await _db.Anuncios
            .Where(ad => ad.iduser == UserId && ad.modelo.StartsWith("[DEMO] "))
            .ToListAsync();
        if (demoAds.Count == 0)
        {
            TempData["Error"] = "Não há anúncios de exemplo para apagar nesta conta.";
            return RedirectToAction(nameof(Index));
        }

        var demoAdIds = demoAds.Select(ad => ad.id).ToArray();
        if (await RentalDeletionGuard.HasOpenRentalsAsync(_db, demoAdIds))
        {
            TempData["Error"] = RentalDeletionGuard.BlockedMessage;
            return RedirectToAction(nameof(Index));
        }

        var demoItemIds = demoAds.Select(ad => ad.itemid).Distinct().ToArray();
        var orphanDemoItems = await _db.Items.Where(item =>
            demoItemIds.Contains(item.id) && item.iduser == UserId &&
            item.nmitem.StartsWith("[DEMO] ") &&
            !_db.Anuncios.Any(other => other.itemid == item.id && !demoAdIds.Contains(other.id)))
            .ToListAsync();

        try
        {
            _db.Anuncios.RemoveRange(demoAds);
            _db.Items.RemoveRange(orphanDemoItems);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Foram apagados {demoAds.Count} anúncios fictícios.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not delete demo rental listings.");
            TempData["Error"] = "Não foi possível apagar todos os anúncios de exemplo.";
        }

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        // Guests can inspect public listing details before deciding to register.
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .Include(a => a.categoria).Include(a => a.tipo)
            .FirstOrDefaultAsync(a => a.id == id);
        if (ad == null || ad.item == null) return NotFound();
        if (ad.Pais != "PT" && ad.iduser != User.FindFirstValue(ClaimTypes.NameIdentifier)) return NotFound();
        if (ad.item.snativo == "N" && ad.iduser != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return NotFound();
        var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(viewerId))
            ViewData["IsFavorite"] = await _db.Favoritos.AsNoTracking()
                .AnyAsync(f => f.UserId == viewerId && f.AnuncioId == ad.id);
        return View(ad);
    }

    public async Task<IActionResult> Create(int? itemId)
    {
        await PrepareCreateOptions(itemId);
        return View(new Anuncio { itemid = itemId ?? 0, Pais = "PT" });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetTipos(int id)
    {
        if (id <= 0) return Json(Array.Empty<SelectListItem>());
        var types = await _db.Tipos.AsNoTracking().Where(t => t.categoriaid == id)
            .OrderBy(t => t.nmtipo)
            .Select(t => new SelectListItem { Value = t.id.ToString(), Text = t.nmtipo })
            .ToListAsync();
        return Json(types);
    }

    private void NormalizeLocation(Anuncio ad)
    {
        ad.Pais = (ad.Pais ?? "").Trim().ToUpperInvariant();
        ad.Estado = (ad.Estado ?? "").Trim();
        if (ad.Pais == LocationCatalog.Brazil) ad.Estado = ad.Estado.ToUpperInvariant();
        ad.Cidade = (ad.Cidade ?? "").Trim();
        ad.Bairro = (ad.Bairro ?? "").Trim();
        ad.Rua = (ad.Rua ?? "").Trim();
        ad.CEP = (ad.CEP ?? "").Trim();
        ad.modelo = (ad.modelo ?? "").Trim();
        ad.contato = (ad.contato ?? "").Trim();
        ad.NomeFantasia = (ad.NomeFantasia ?? "").Trim();

        if (ad.Pais != "PT")
            ModelState.AddModelError(nameof(ad.Pais), "Nesta fase, a RentAll aceita apenas anúncios de Portugal.");
        if (!LocationCatalog.IsValidRegion(ad.Pais, ad.Estado))
            ModelState.AddModelError(nameof(ad.Estado), "Selecione um estado ou distrito válido para o país.");
        bool validPostal = ad.Pais switch
        {
            LocationCatalog.Brazil => System.Text.RegularExpressions.Regex.IsMatch(ad.CEP, @"^\d{5}-?\d{3}$"),
            LocationCatalog.Portugal => System.Text.RegularExpressions.Regex.IsMatch(ad.CEP, @"^\d{4}-?\d{3}$"),
            _ => false
        };
        if (!string.IsNullOrWhiteSpace(ad.CEP) && !validPostal)
            ModelState.AddModelError(nameof(ad.CEP), "Código postal inválido para o país selecionado.");
        if (string.IsNullOrWhiteSpace(ad.Cidade))
            ModelState.AddModelError(nameof(ad.Cidade), "Informe a cidade em que o item está disponível.");
        // Bairro e morada não são necessários para anunciar.
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int Categoria, int Tipo, Anuncio anuncio)
    {
        anuncio.iduser = UserId;
        anuncio.Pais = (anuncio.Pais ?? "").Trim().ToUpperInvariant();
        anuncio.categoriaid = Categoria;
        anuncio.tipoid = Tipo;
        ModelState.Remove(nameof(Anuncio.iduser));
        ModelState.Remove(nameof(Anuncio.categoriaid));
        ModelState.Remove(nameof(Anuncio.tipoid));
        NormalizeLocation(anuncio);

        if (!await _db.Items.AnyAsync(x => x.id == anuncio.itemid && x.iduser == UserId && x.snativo != "N"))
            ModelState.AddModelError(nameof(anuncio.itemid), "Selecione um item ativo cadastrado na sua conta.");
        if (!await _db.Categorias.AnyAsync(x => x.id == Categoria))
            ModelState.AddModelError(nameof(Categoria), "Selecione uma categoria.");
        if (!await _db.Tipos.AnyAsync(x => x.id == Tipo && x.categoriaid == Categoria))
            ModelState.AddModelError(nameof(Tipo), "Selecione um tipo válido para esta categoria.");

        if (MarketplaceContactSafety.ContainsContactOrRedirect(anuncio.modelo) ||
            MarketplaceContactSafety.ContainsContactOrRedirect(anuncio.NomeFantasia))
            ModelState.AddModelError("", "Não incluas contactos, links ou redes sociais nos campos públicos do anúncio.");
        if (!ModelState.IsValid)
        {
            await PrepareCreateOptions(anuncio.itemid);
            return View(anuncio);
        }

        // Bind only user-controlled content. Owner/category associations are validated server-side.
        var ad = new Anuncio
        {
            vlitem = anuncio.vlitem, diasAluguel = anuncio.diasAluguel,
            modelo = anuncio.modelo, itemid = anuncio.itemid, iduser = UserId, Pais = anuncio.Pais,
            categoriaid = Categoria, tipoid = Tipo,
            // Legacy ad wizard also avoids collecting exact address details.
            CEP = "", Rua = "", Numero = null,
            Bairro = anuncio.Bairro, Cidade = anuncio.Cidade, Estado = anuncio.Estado,
            Complemento = null, contato = "",
            NomeFantasia = string.IsNullOrWhiteSpace(anuncio.NomeFantasia) ? "Anunciante RentAll" : anuncio.NomeFantasia
        };
        try
        {
            _db.Anuncios.Add(ad);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Anúncio publicado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not create rental listing.");
            ModelState.AddModelError("", "Não foi possível publicar o anúncio. Tente novamente.");
            await PrepareCreateOptions(anuncio.itemid);
            return View(anuncio);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var ad = await _db.Anuncios.FirstOrDefaultAsync(a => a.id == id && a.iduser == UserId);
        if (ad?.Pais == "BR") { TempData["Error"] = "A edição de anúncios históricos do Brasil está temporariamente indisponível."; return RedirectToAction(nameof(Index)); }
        return ad is null ? NotFound() : View(ad);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Anuncio anuncio)
    {
        if (id != anuncio.id) return NotFound();
        var existing = await _db.Anuncios.FirstOrDefaultAsync(a => a.id == id && a.iduser == UserId);
        if (existing is null) return NotFound();
        if (existing.Pais != "PT") { TempData["Error"] = "Os anúncios históricos fora de Portugal não podem ser alterados nesta fase."; return RedirectToAction(nameof(Index)); }

        anuncio.iduser = existing.iduser;
        anuncio.Pais = (anuncio.Pais ?? "").Trim().ToUpperInvariant();
        anuncio.itemid = existing.itemid;
        anuncio.categoriaid = existing.categoriaid;
        anuncio.tipoid = existing.tipoid;
        ModelState.Remove(nameof(Anuncio.iduser));
        ModelState.Remove(nameof(Anuncio.itemid));
        ModelState.Remove(nameof(Anuncio.categoriaid));
        ModelState.Remove(nameof(Anuncio.tipoid));
        NormalizeLocation(anuncio);
        if (MarketplaceContactSafety.ContainsContactOrRedirect(anuncio.modelo) ||
            MarketplaceContactSafety.ContainsContactOrRedirect(anuncio.NomeFantasia))
            ModelState.AddModelError("", "Remove contactos pessoais e links do anúncio. Comunica através da RentAll.");
        if (!ModelState.IsValid) return View(anuncio);

        // The owner, linked item, category and type cannot be changed via hidden fields.
        existing.vlitem = anuncio.vlitem;
        existing.diasAluguel = anuncio.diasAluguel;
        existing.modelo = anuncio.modelo;
        existing.Pais = anuncio.Pais;
        existing.CEP = "";
        existing.Rua = "";
        existing.Numero = null;
        existing.Bairro = anuncio.Bairro;
        existing.Cidade = anuncio.Cidade;
        existing.Estado = anuncio.Estado;
        existing.Complemento = null;
        existing.contato = "";
        existing.NomeFantasia = anuncio.NomeFantasia;

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Anúncio atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not update rental listing.");
            ModelState.AddModelError("", "Não foi possível guardar as alterações. Tente novamente.");
            return View(anuncio);
        }
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var ad = await _db.Anuncios.Include(a => a.item)
            .FirstOrDefaultAsync(a => a.id == id && a.iduser == UserId);
        return ad is null ? NotFound() : View(ad);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ad = await _db.Anuncios.FirstOrDefaultAsync(a => a.id == id && a.iduser == UserId);
        if (ad is null) return NotFound();
        if (await aluguel.Services.RentalDeletionGuard.HasOpenRentalsAsync(_db, new[] { ad.id }))
        {
            TempData["Error"] = aluguel.Services.RentalDeletionGuard.BlockedMessage;
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var demoItem = await _db.Items.FirstOrDefaultAsync(item =>
                item.id == ad.itemid && item.iduser == UserId && item.nmitem.StartsWith("[DEMO] "));
            _db.Anuncios.Remove(ad);
            if (demoItem != null && !await _db.Anuncios.AnyAsync(other =>
                other.itemid == demoItem.id && other.id != ad.id))
                _db.Items.Remove(demoItem);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Anúncio excluído.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not delete rental listing.");
            TempData["Error"] = "Não foi possível excluir este anúncio porque há registos associados.";
            return RedirectToAction(nameof(Index));
        }
    }
}
