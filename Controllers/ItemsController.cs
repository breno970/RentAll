using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ILogger<ItemsController> _logger;
        private readonly ApplicationDbContext Context;
        private readonly IWebHostEnvironment WebHostEnvironment;

        public ItemsController(ILogger<ItemsController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            Context = context;
            WebHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var model = await Context.Items
                            .Where(a => a.iduser == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value)
                            .ToListAsync();
            return View(model);

        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || Context.Items == null)
            {
                return NotFound();
            }

            var item = await Context.Items
                .FirstOrDefaultAsync(m => m.id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult GetTipos(int id)
        {
            //get state list as SelectListItem
            var statesList = Context.Tipos.Where(s => s.categoriaid == id).ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmtipo }).ToList();
            return Json(statesList);
        }

        [HttpPost]
        public IActionResult Create(ItemViewModel vm)
        {
            string imagem1 = Imagem1(vm);
            string? imagem2 = Imagem2(vm);
            string? imagem3 = Imagem3(vm);
            string? imagem4 = Imagem4(vm);
            string? imagem5 = Imagem5(vm);
            var item = new Item
            {
                nmitem = vm.nmitem,
                dsitem = vm.dsitem,
                dtcriacao = vm.dtcriacao = DateTime.Now,
                snativo = vm.snativo,
                iduser = vm.iduser,
                imagem1 = imagem1,
                imagem2 = imagem2,
                imagem3 = imagem3,
                imagem4 = imagem4,
                imagem5 = imagem5

            };
            ViewBag.Categoria = Context.Categorias.ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmcategoria }).ToList();

            Context.Items.Add(item);
            Context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || Context.Items == null)
            {
                return NotFound();
            }

            var item = await Context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,nmitem,dsitem,dtcriacao,snativo,imagem1,imagem2,imagem3,imagem4,imagem5,iduser")] Item item)
        {
            if (id != item.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Context.Update(item);
                    await Context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || Context.Items == null)
            {
                return NotFound();
            }

            var item = await Context.Items
                .FirstOrDefaultAsync(m => m.id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (Context.Items == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Items'  is null.");
            }
            var item = await Context.Items.FindAsync(id);
            if (item != null)
            {
                Context.Items.Remove(item);
            }

            await Context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private string Imagem1(ItemViewModel vm)
        {
            string imagem = null;
            if (vm.imagem1 != null)
            {
                string uploadDir = Path.Combine(WebHostEnvironment.WebRootPath, "imagens");
                imagem = Guid.NewGuid().ToString() + "-" + vm.imagem1.FileName;
                string filePath = Path.Combine(uploadDir, imagem);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.imagem1.CopyTo(fileStream);
                }

            }
            return imagem;

        }

        private string Imagem2(ItemViewModel vm)
        {
            string imagem = null;
            if (vm.imagem2 != null)
            {
                string uploadDir = Path.Combine(WebHostEnvironment.WebRootPath, "imagens");
                imagem = Guid.NewGuid().ToString() + "-" + vm.imagem2.FileName;
                string filePath = Path.Combine(uploadDir, imagem);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.imagem2.CopyTo(fileStream);
                }

            }
            return imagem;

        }

        private string Imagem3(ItemViewModel vm)
        {
            string imagem = null;
            if (vm.imagem3 != null)
            {
                string uploadDir = Path.Combine(WebHostEnvironment.WebRootPath, "imagens");
                imagem = Guid.NewGuid().ToString() + "-" + vm.imagem3.FileName;
                string filePath = Path.Combine(uploadDir, imagem);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.imagem3.CopyTo(fileStream);
                }

            }
            return imagem;

        }

        private string Imagem4(ItemViewModel vm)
        {
            string imagem = null;
            if (vm.imagem4 != null)
            {
                string uploadDir = Path.Combine(WebHostEnvironment.WebRootPath, "imagens");
                imagem = Guid.NewGuid().ToString() + "-" + vm.imagem4.FileName;
                string filePath = Path.Combine(uploadDir, imagem);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.imagem4.CopyTo(fileStream);
                }

            }
            return imagem;

        }

        private string Imagem5(ItemViewModel vm)
        {
            string imagem = null;
            if (vm.imagem5 != null)
            {
                string uploadDir = Path.Combine(WebHostEnvironment.WebRootPath, "imagens");
                imagem = Guid.NewGuid().ToString() + "-" + vm.imagem5.FileName;
                string filePath = Path.Combine(uploadDir, imagem);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.imagem5.CopyTo(fileStream);
                }

            }
            return imagem;

        }

        private bool ItemExists(int id)
        {
            return (Context.Items?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
