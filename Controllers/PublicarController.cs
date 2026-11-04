using System.Globalization;
using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers;

/// <summary>Publica um objeto e o seu anúncio numa única operação.</summary>
[Authorize]
[Route("Publicar")]
public sealed class PublicarController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IImageStorage _images;
    private readonly ILogger<PublicarController> _logger;

    public PublicarController(ApplicationDbContext db, IImageStorage images, ILogger<PublicarController> logger)
    {
        _db = db;
        _images = images;
        _logger = logger;
    }

    private string Owner => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    private async Task LoadCategories()
    {
        ViewBag.Categoria = await _db.Categorias.AsNoTracking().OrderBy(x => x.nmcategoria)
            .Select(x => new SelectListItem { Value = x.id.ToString(), Text = x.nmcategoria }).ToListAsync();
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? Pais)
    {
        await LoadCategories();
        return View(new PublishViewModel { Pais = Pais == "BR" ? "BR" : "PT" });
    }

    private static bool ParseMoney(string? raw, out float amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        var text = raw.Trim().Replace("R$", "").Replace("€", "")
            .Replace(" ", "").Replace("\u00a0", "");
        int comma = text.LastIndexOf(',');
        int dot = text.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
            text = comma > dot ? text.Replace(".", "").Replace(",", ".") : text.Replace(",", "");
        else if (comma >= 0)
            text = text.Replace(",", ".");
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value) || value < 0 || value > 10000000)
            return false;
        amount = (float)value;
        return true;
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(35_000_000)]
    [EnableRateLimiting("publish")]
    public async Task<IActionResult> Index(PublishViewModel vm, CancellationToken ct)
    {
        vm.Photos ??= new();
        vm.Pais = (vm.Pais ?? "").Trim().ToUpperInvariant();
        vm.Estado = vm.Estado?.Trim();
        vm.Cidade = vm.Cidade?.Trim();

        if (string.IsNullOrWhiteSpace(vm.Nome) || vm.Nome.Length > 140)
            ModelState.AddModelError(nameof(vm.Nome), "Indica o nome do objeto (até 140 caracteres).");
        if (string.IsNullOrWhiteSpace(vm.Descricao) || vm.Descricao.Length > 6000)
            ModelState.AddModelError(nameof(vm.Descricao), "Descreve o objeto (até 6000 caracteres).");
        if (!LocationCatalog.IsValidCountry(vm.Pais) || !LocationCatalog.IsValidRegion(vm.Pais, vm.Estado))
            ModelState.AddModelError(nameof(vm.Estado), "Seleciona um país e uma região válidos.");
        if (string.IsNullOrWhiteSpace(vm.Cidade) || vm.Cidade.Length > 120)
            ModelState.AddModelError(nameof(vm.Cidade), "Indica apenas a cidade ou o município.");
        if (vm.DiasAluguel is < 1 or > 365)
            ModelState.AddModelError(nameof(vm.DiasAluguel), "Indica um período entre 1 e 365 dias, ou deixa em branco.");
        if (!ParseMoney(vm.Preco, out var price))
            ModelState.AddModelError(nameof(vm.Preco), "Indica um preço válido em euros ou reais, ou deixa em branco.");
        if (vm.Photos.Count is < 1 or > 5)
            ModelState.AddModelError(nameof(vm.Photos), "Seleciona entre 1 e 5 fotografias.");
        var order = (vm.PhotoOrder ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (order.Length != vm.Photos.Count ||
            order.Distinct(StringComparer.Ordinal).Count() != vm.Photos.Count ||
            order.Where((token, i) => true).Any(t => !t.StartsWith("n:", StringComparison.Ordinal) ||
                !int.TryParse(t[2..], out var index) || index < 0 || index >= vm.Photos.Count))
            ModelState.AddModelError(nameof(vm.Photos), "A ordem das fotografias é inválida. Seleciona-as novamente.");
        if (!await _db.Categorias.AnyAsync(x => x.id == vm.Categoria, ct))
            ModelState.AddModelError(nameof(vm.Categoria), "Escolhe uma categoria.");
        if (!await _db.Tipos.AnyAsync(x => x.id == vm.Tipo && x.categoriaid == vm.Categoria, ct))
            ModelState.AddModelError(nameof(vm.Tipo), "Escolhe um tipo para a categoria selecionada.");

        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return View(vm);
        }

        var stored = new List<string>();
        try
        {
            foreach (var file in vm.Photos)
                stored.Add(await _images.UploadAsync(file, Owner, ct));

            // A ordem é controlada pelo carrossel; apenas índices de ficheiros recém-enviados.
            var arranged = order.Select(t => stored[int.Parse(t[2..], CultureInfo.InvariantCulture)]).ToList();
            var item = new Item
            {
                nmitem = vm.Nome!.Trim(), dsitem = vm.Descricao!.Trim(),
                dtcriacao = DateTime.UtcNow, snativo = vm.Visibilidade == "N" ? "N" : "S",
                iduser = Owner,
                imagem1 = arranged[0],
                imagem2 = arranged.ElementAtOrDefault(1),
                imagem3 = arranged.ElementAtOrDefault(2),
                imagem4 = arranged.ElementAtOrDefault(3),
                imagem5 = arranged.ElementAtOrDefault(4)
            };
            var ad = new Anuncio
            {
                item = item,
                iduser = Owner, categoriaid = vm.Categoria, tipoid = vm.Tipo,
                vlitem = price, diasAluguel = vm.DiasAluguel ?? 0,
                modelo = (vm.Modelo ?? "").Trim(),
                Pais = vm.Pais, Estado = vm.Estado!, Cidade = vm.Cidade!,
                Bairro = (vm.Bairro ?? "").Trim(),
                // Never persist a precise address: the service only needs the municipality.
                CEP = "", Rua = "", Numero = null, Complemento = null,
                contato = "",
                NomeFantasia = string.IsNullOrWhiteSpace(vm.NomePublico)
                    ? "Anunciante RentAll" : vm.NomePublico.Trim()
            };
            _db.Anuncios.Add(ad);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Anúncio publicado! A localização exata e os contactos não foram recolhidos.";
            return item.snativo == "N"
                ? RedirectToAction("Index", "Anuncios")
                : RedirectToAction("Details", "Anuncios", new { id = ad.id });
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or HttpRequestException
                                      or InvalidOperationException or DbUpdateException)
        {
            _logger.LogError(ex, "Falha ao publicar artigo com fotografias");
            foreach (var image in stored)
            {
                try { await _images.DeleteAsync(image, CancellationToken.None); }
                catch (Exception cleanup) { _logger.LogWarning(cleanup, "Falha ao limpar imagem não associada."); }
            }
            ModelState.AddModelError("", ex is ImageStorageException imageError
                ? imageError.UserMessage
                : ex is InvalidDataException ? ex.Message
                : "Não foi possível publicar. Verifica o serviço de imagens ou a ligação à base de dados.");
            await LoadCategories();
            return View(vm);
        }
    }
}
