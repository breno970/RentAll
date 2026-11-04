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

    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var ad = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .Include(a => a.categoria).Include(a => a.tipo)
            .FirstOrDefaultAsync(a => a.id == id);
        if (ad == null || ad.item == null) return NotFound();
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
        return View(new Anuncio { itemid = itemId ?? 0 });
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

        if (!LocationCatalog.IsValidCountry(ad.Pais))
            ModelState.AddModelError(nameof(ad.Pais), "Selecione Brasil ou Portugal.");
        if (!LocationCatalog.IsValidRegion(ad.Pais, ad.Estado))
            ModelState.AddModelError(nameof(ad.Estado), "Selecione um estado ou distrito válido para o país.");
        bool validPostal = ad.Pais switch
        {
            LocationCatalog.Brazil => System.Text.RegularExpressions.Regex.IsMatch(ad.CEP, @"^\d{5}-?\d{3}$"),
            LocationCatalog.Portugal => System.Text.RegularExpressions.Regex.IsMatch(ad.CEP, @"^\d{4}-?\d{3}$"),
            _ => false
        };
        if (!validPostal)
            ModelState.AddModelError(nameof(ad.CEP), "Código postal inválido para o país selecionado.");
        if (string.IsNullOrWhiteSpace(ad.Cidade))
            ModelState.AddModelError(nameof(ad.Cidade), "Informe a cidade em que o item está disponível.");
        if (string.IsNullOrWhiteSpace(ad.Bairro))
            ModelState.AddModelError(nameof(ad.Bairro), "Informe o bairro.");
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
            CEP = anuncio.CEP, Rua = anuncio.Rua, Numero = anuncio.Numero,
            Bairro = anuncio.Bairro, Cidade = anuncio.Cidade, Estado = anuncio.Estado,
            Complemento = anuncio.Complemento, contato = anuncio.contato,
            NomeFantasia = anuncio.NomeFantasia
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
        return ad is null ? NotFound() : View(ad);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Anuncio anuncio)
    {
        if (id != anuncio.id) return NotFound();
        var existing = await _db.Anuncios.FirstOrDefaultAsync(a => a.id == id && a.iduser == UserId);
        if (existing is null) return NotFound();

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
        if (!ModelState.IsValid) return View(anuncio);

        // The owner, linked item, category and type cannot be changed via hidden fields.
        existing.vlitem = anuncio.vlitem;
        existing.diasAluguel = anuncio.diasAluguel;
        existing.modelo = anuncio.modelo;
        existing.Pais = anuncio.Pais;
        existing.CEP = anuncio.CEP;
        existing.Rua = anuncio.Rua;
        existing.Numero = anuncio.Numero;
        existing.Bairro = anuncio.Bairro;
        existing.Cidade = anuncio.Cidade;
        existing.Estado = anuncio.Estado;
        existing.Complemento = anuncio.Complemento;
        existing.contato = anuncio.contato;
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
        try
        {
            _db.Anuncios.Remove(ad);
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
