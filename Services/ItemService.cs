using aluguel.Areas.Identity.Data;
using aluguel.IServices;
using aluguel.Models;
using Microsoft.EntityFrameworkCore;

namespace aluguel.Services;

/// <summary>Items are always queried or saved for an explicitly authenticated owner.</summary>
public sealed class ItemService : IItemService
{
    private readonly ApplicationDbContext _db;

    public ItemService(ApplicationDbContext db) => _db = db;

    public Task<Item?> GetOwnedAsync(int itemId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException();
        return _db.Items.AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == itemId && x.iduser == userId, ct);
    }

    public async Task<Item> SaveAsync(Item item, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException();
        if (item.iduser != userId) throw new UnauthorizedAccessException("Item ownership mismatch.");
        if (item.id != 0) throw new InvalidOperationException("SaveAsync can only create new items.");
        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }
}
