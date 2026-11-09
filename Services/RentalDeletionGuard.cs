using aluguel.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace aluguel.Services;

/// <summary>
/// Listings and items cascade-delete their rental requests, messages and demo journeys
/// (database FKs use ON DELETE CASCADE). Deleting while a rental is still open would silently
/// erase the OTHER participant's records, so deletion is refused until it is closed.
/// </summary>
public static class RentalDeletionGuard
{
    public const string BlockedMessage =
        "Este anúncio tem pedidos ou alugueres em curso. Oculta-o em vez de o eliminar, " +
        "para que a outra pessoa não perca o histórico. Podes eliminá-lo depois de concluídos ou cancelados.";

    public static readonly string[] OpenRequestStates = { "Pendente", "Contraproposta" };
    public static readonly string[] ClosedJourneyStates = { "Concluido", "Cancelado" };

    public static async Task<bool> HasOpenRentalsAsync(ApplicationDbContext db,
        IReadOnlyCollection<int> anuncioIds, CancellationToken ct = default)
    {
        if (anuncioIds.Count == 0) return false;
        if (await db.PedidosAluguer.AnyAsync(p => anuncioIds.Contains(p.AnuncioId) &&
                OpenRequestStates.Contains(p.Estado), ct))
            return true;
        // An accepted request stays open until its journey is concluded or cancelled.
        return await db.PedidosAluguer
            .Where(p => anuncioIds.Contains(p.AnuncioId) && p.Estado == "Aceite sem pagamento")
            .AnyAsync(p => !db.PercursosAluguerDemo.Any(d => d.PedidoId == p.Id &&
                ClosedJourneyStates.Contains(d.Estado)), ct);
    }
}
