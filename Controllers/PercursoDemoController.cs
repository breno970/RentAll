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
    private readonly IImageStorage _images;
    // Becomes true once the additive migration 014 is detected; never re-checked afterwards.
    private static volatile bool _historicoConfirmado;

    public PercursoDemoController(ApplicationDbContext db, ILogger<PercursoDemoController> logger,
        IImageStorage images)
    {
        _db = db;
        _logger = logger;
        _images = images;
    }

    /// <summary>
    /// The logistics trail lives in rentall."PercursoDemoEventos" (database/014). Until that
    /// migration is applied the journey keeps working without checklists or history notes.
    /// </summary>
    private async Task<bool> HistoricoDisponivelAsync(CancellationToken ct)
    {
        if (_historicoConfirmado) return true;
        try
        {
            await _db.PercursoDemoEventos.AsNoTracking().Select(e => e.Id).FirstOrDefaultAsync(ct);
            _historicoConfirmado = true;
            return true;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "42P01" or "42501")
        {
            _logger.LogWarning("Demo logistics trail unavailable ({SqlState}). Apply database/014.", ex.SqlState);
            return false;
        }
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
        var historico = await HistoricoDisponivelAsync(ct);
        var eventos = historico && percurso != null
            ? await _db.PercursoDemoEventos.AsNoTracking().Where(e => e.PedidoId == pedidoId)
                .OrderBy(e => e.CriadoEm).ThenBy(e => e.Id).Take(200).ToListAsync(ct)
            : new List<PercursoDemoEvento>();
        return View(new PercursoDemoViewModel
        {
            Pedido = pedido,
            Percurso = percurso,
            Artigo = titulo ?? "Aluguer RentAll",
            SouAnunciante = pedido.AnuncianteId == Me,
            HistoricoDisponivel = historico,
            Eventos = eventos
        });
    }

    [HttpPost("Iniciar")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rental-journey")]
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
    [EnableRateLimiting("rental-journey")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> Atualizar(int pedidoId, string operacao,
        string? metodo, string? tipo, string? local, DateTime? dataHora, int? avaliacao,
        string? nota, string? estadoArtigo, string? acessorios, IFormFile? foto,
        CancellationToken ct)
    {
        operacao = operacao?.Trim() ?? "";
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

        var historico = await HistoricoDisponivelAsync(ct);
        if (PercursoDemoRegras.ExigeHistorico(operacao) && !historico)
        {
            TempData["Error"] = "Esta funcionalidade fica disponível após a próxima atualização da base de dados.";
            return RedirectToAction(nameof(Detalhes), new { pedidoId });
        }
        // Notes are visible to the other participant: keep contacts inside RentAll.
        if (MarketplaceContactSafety.ContainsContactOrRedirect(nota) ||
            MarketplaceContactSafety.ContainsContactOrRedirect(acessorios) ||
            MarketplaceContactSafety.ContainsContactOrRedirect(local))
        {
            TempData["Error"] = "Retira telefones, emails ou links. Usa as mensagens da RentAll para combinar detalhes.";
            return RedirectToAction(nameof(Detalhes), new { pedidoId });
        }
        bool souAnunciante = pedido.AnuncianteId == Me;
        string? fotoKey = null;
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
            var agora = DateTime.UtcNow;
            var message = PercursoDemoRegras.Atualizar(percurso, operacao,
                souAnunciante, agora, metodo, tipo, local,
                entregaUtc, avaliacao, nota, estadoArtigo, acessorios);
            if (foto is { Length: > 0 })
            {
                if (operacao is not ("checklist_entrega" or "checklist_devolucao"))
                    throw new InvalidOperationException("Só é possível anexar fotografias às checklists.");
                fotoKey = await _images.UploadAsync(foto, Me, ct);
            }
            if (historico)
            {
                bool comDetalhe = operacao is "checklist_entrega" or "checklist_devolucao";
                _db.PercursoDemoEventos.Add(new PercursoDemoEvento
                {
                    PedidoId = pedidoId,
                    AutorId = Me,
                    Papel = souAnunciante ? "anunciante" : "locatario",
                    Operacao = operacao,
                    EstadoArtigo = comDetalhe ? estadoArtigo : null,
                    Acessorios = comDetalhe ? acessorios?.Trim() : null,
                    Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
                    FotoKey = fotoKey,
                    CriadoEm = agora
                });
            }
            await _db.SaveChangesAsync(ct);
            fotoKey = null; // persisted: keep the image
            TempData["Success"] = message;
        }
        catch (InvalidDataException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ImageStorageException ex)
        {
            TempData["Error"] = ex.UserMessage;
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Photo upload failed for rental demo {PedidoId}", pedidoId);
            TempData["Error"] = "Não foi possível enviar a fotografia. Tenta novamente ou guarda a checklist sem fotografia.";
        }
        finally
        {
            // An uploaded photo whose record was not saved must not stay orphaned in storage.
            if (fotoKey != null)
            {
                try { await _images.DeleteAsync(fotoKey, CancellationToken.None); }
                catch (Exception cleanup) { _logger.LogWarning(cleanup, "Could not remove orphan checklist photo."); }
            }
        }
        return RedirectToAction(nameof(Detalhes), new { pedidoId });
    }
}
