using aluguel.Areas.Identity.Data;
using aluguel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aluguel.Controllers;

/// <summary>
/// Read-only same-origin API for forms and marketplace discovery. Never exposes account details.
/// External integrations (payments/Google) require separately configured provider credentials.
/// </summary>
[ApiController]
[Route("api/v1/catalogo")]
[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Client, VaryByQueryKeys = new[] { "*" })]
public sealed class CatalogoApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CatalogoApiController(ApplicationDbContext db) => _db = db;

    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias(CancellationToken ct)
    {
        var rows = await _db.Categorias.AsNoTracking().OrderBy(c => c.nmcategoria)
            .Select(c => new { id = c.id, nome = c.nmcategoria })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos([FromQuery] int categoriaId, CancellationToken ct)
    {
        if (categoriaId < 1) return BadRequest(new { erro = "Indica uma categoria válida." });
        var rows = await _db.Tipos.AsNoTracking().Where(t => t.categoriaid == categoriaId)
            .OrderBy(t => t.nmtipo)
            .Select(t => new { id = t.id, nome = t.nmtipo }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("regioes")]
    public IActionResult Regioes([FromQuery] string pais = "PT")
    {
        pais = pais.Trim().ToUpperInvariant();
        if (!LocationCatalog.IsValidCountry(pais))
            return BadRequest(new { erro = "País inválido. Escolhe PT ou BR." });
        return Ok(LocationCatalog.RegionsFor(pais).Select(r => new { codigo = r.Code, nome = r.Name }));
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] string pais = "PT", CancellationToken ct = default)
    {
        pais = pais.Trim().ToUpperInvariant();
        if (!LocationCatalog.IsValidCountry(pais))
            return BadRequest(new { erro = "País inválido." });
        var published = _db.Anuncios.AsNoTracking().Where(a => a.Pais == pais &&
            a.item != null && a.item.snativo != "N");
        // Aggregate statistics only; no names, exact addresses, or hidden listings.
        var listings = await published.CountAsync(ct);
        var cities = await published.Select(a => a.Cidade).Distinct().CountAsync(ct);
        return Ok(new { pais, anunciosPublicos = listings, cidades = cities });
    }
}
