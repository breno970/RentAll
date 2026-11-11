using aluguel.Models;

namespace aluguel.IServices;

public interface IItemService
{
    Task<Item?> GetOwnedAsync(int itemId, string userId, CancellationToken ct = default);
    Task<Item> SaveAsync(Item item, string userId, CancellationToken ct = default);
}
