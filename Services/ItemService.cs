using aluguel.Areas.Identity.Data;
using aluguel.IServices;
using aluguel.Models;

namespace aluguel.Services
{
    public class ItemService : IItemService
    {
        private readonly ApplicationDbContext _context;

        public ItemService(ApplicationDbContext context)
        {
            _context = context;
        }
        public Item GetSavedItem()
        {
            return _context.Items.SingleOrDefault();
        }

        public Item Save(Item item)
        {
            _context.Items.Add(item);
            _context.SaveChanges();
            return item;
        }
    }
}
