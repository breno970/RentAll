using aluguel.Models;

namespace aluguel.IServices
{
    public interface IItemService
    {
        Item Save(Item item);
        Item GetSavedItem();
    }
}
