using System.Security.Claims;
using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace aluguel.Controllers;

[Authorize]
[Route("PercursoDemo")]
public sealed class PercursoDemoController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PercursoDemoController> _logger;

    public PercursoDemoController(ApplicationDbContext db, ILogger<PercursoDemoController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    private Task<PedidoAluguer?> PedidoAutorizado(int pedidoId, CancellationToken ct) =>
        _db.PedidosAluguer.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pedidoId &&
                (p.InteressadoId == Me || p.AnuncianteId == Me) && p.Pais == "PT", ct);

    [HttpGet("{pedidoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detalhes(int pedidoId, CancellationToken ct)
    {
        var pedido = await PedidoAutorizado(pedidoId, ct);
        if (pedido == null) return NotFound();
        var percurso = await _db.PercursosAluguerDemo.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PedidoId == pedidoId, ct);
        var titulo = await _db.Anuncios.AsNoTracking().Include(a => a.item)
            .Where(a => a.id == pedido.AnuncioId)
            .Select(a => a.item != null ? a.item.nmitem : a.modelo)
            .FirstOrDefaultAsync(ct);
        return View(new PercursoDemoViewModel
        {
            Pedido = pedido,
            Percurso = percurso,
            Artigo = titulo ?? "Aluguer RentAll",
            SouAnunciante = pedido.AnuncianteId == Me
        });
    }

    [HttpPost("Iniciar")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Iniciar(int pedidoId, CancellationToken ct)
    {
        var pedido = await PedidoAutorizado(pedidoId, ct);
        if (pedido == null) return NotFound();
        if (pedido.InteressadoId != Me || pedido.Estado != "Aceite sem pagamento")
        {
            TempData["Error"] = "O percurso só pode ser iniciado pelo locatário após aceitação do pedido.";
            return RedirectToAction(nameof(Detalhes), new { pedidoId });
        }

        if (!await _db.PercursosAluguerDemo.AnyAsync(x => x.PedidoId == pedidoId, ct))
        {
            _db.PercursosAluguerDemo.Add(new PercursoAluguerDemo { PedidoId = pedidoId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex)
            {
                // The primary key prevents duplicate creation during simultaneous submissions.
                _logger.LogWarning(ex, "Could not start demo for rental {PedidoId}", pedidoId);
                TempData["Error"] = "O percurso já pode ter sido iniciado. Atualiza a página para consultar o estado.";
                return RedirectToAction(nameof(Detalhes), new { pedidoId });
            }
        }
        TempData["Success"] = "Percurso de demonstração iniciado. O pedido financeiro real não foi alterado.";
        return RedirectToAction(nameof(Detalhes), new { pedidoId });
    }

    [HttpPost("Atualizar")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-requests")]
    public async Task<IActionResult> Atualizar(int pedidoId, string operacao,
        string? metodo, string? tipo, string? local, DateTime? dataHora, int? avaliacao,
        CancellationToken ct)
    {
        var pedido = await PedidoAutorizado(pedidoId, ct);
        if (pedido == null) return NotFound();
        if (pedido.Estado != "Aceite sem pagamento")
        {
            TempData["Error"] = "Este pedido deixou de estar elegível para demonstração.";
            return RedirectToAction(nameof(Detalhes), new { pedidoId });
        }
        var percurso = await _db.PercursosAluguerDemo
            .FirstOrDefaultAsync(x => x.PedidoId == pedidoId, ct);
        if (percurso == null) return NotFound();

        DateTime? entregaUtc = null;
        try
        {
            if (operacao == "propor" && dataHora.HasValue)
            {
                // The RentAll market is Portugal. datetime-local has no timezone marker.
                // Convert the supplied Lisbon wall-clock time explicitly to UTC for PostgreSQL.
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
                var date = DateTime.SpecifyKind(dataHora.Value, DateTimeKind.Unspecified);
                if (tz.IsInvalidTime(date))
                    throw new InvalidOperationException("A hora escolhida não existe no horário de Portugal.");
                entregaUtc = TimeZoneInfo.ConvertTimeToUtc(date, tz);
            }
            var message = PercursoDemoRegras.Atualizar(percurso, operacao,
                pedido.AnuncianteId == Me, DateTime.UtcNow, metodo, tipo, local,
                entregaUtc, avaliacao);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = message;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "O percurso foi alterado por outra pessoa. Consulta o estado mais recente.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to persist rental demo {PedidoId}", pedidoId);
            TempData["Error"] = "Não foi possível guardar. Nenhuma transação financeira foi efetuada.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentException)
        {
            TempData["Error"] = "A data de entrega é inválida.";
        }
        return RedirectToAction(nameof(Detalhes), new { pedidoId });
    }
}
