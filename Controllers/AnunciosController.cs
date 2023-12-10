using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers
{
    public class AnunciosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnunciosController(ApplicationDbContext context)
        {
            _context = context;
        }

        //var applicationDbContext = _context.Anuncios.Include(a => a.item);
        //    return View(await applicationDbContext.ToListAsync());



        // GET: Anuncios
        public async Task<IActionResult> Index()
        {
            var model = await _context.Anuncios
                            .Where(a => a.iduser == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value)
                            .Include(a => a.item)
                            .Include(a => a.categoria)
                            .Include(a => a.tipo)
                            .ToListAsync();
            return View(model);

        }

        // GET: Anuncios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Anuncios == null)
            {
                return NotFound();
            }

            var anuncio = await _context.Anuncios
                .Include(a => a.item)
                .Include(a => a.categoria)
                .Include(a => a.tipo)
                .FirstOrDefaultAsync(m => m.id == id);
            if (anuncio == null)
            {
                return NotFound();
            }

            return View(anuncio);
        }

        // GET: Anuncios/Create
        public IActionResult Create()
        {
            ViewBag.Categoria = _context.Categorias.ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmcategoria }).ToList();
            ViewData["itemid"] = new SelectList(_context.Items
                .Where(a => a.iduser == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value && a.snativo != "N"), "id", "nmitem");

            return View();
        }

        public IActionResult GetTipos(int id)
        {
            //get state list as SelectListItem
            var statesList = _context.Tipos.Where(s => s.categoriaid == id).ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmtipo }).ToList();
            return Json(statesList);
        }

        // POST: Anuncios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult Create(int Categoria, int Tipo, Anuncio anuncio)
        {
            ViewBag.SelectCategoria = _context.Categorias.Where(a => a.id == Categoria).FirstOrDefault().nmcategoria;
            ViewBag.SelectTipo = _context.Tipos.Where(a => a.id == Tipo).FirstOrDefault().nmtipo;

            var item = new Anuncio
            {
                vlitem = anuncio.vlitem,
                diasAluguel = anuncio.diasAluguel,
                iduser = anuncio.iduser,
                itemid = anuncio.itemid,
                modelo = anuncio.modelo,
                CEP = anuncio.CEP,
                Rua = anuncio.Rua,
                Numero = anuncio.Numero,
                Bairro = anuncio.Bairro,
                Cidade = anuncio.Cidade,
                Estado = anuncio.Estado,
                Complemento = anuncio.Complemento,
                contato = anuncio.contato,
                NomeFantasia = anuncio.NomeFantasia,
                categoriaid = Categoria,
                tipoid = Tipo
            };

            ViewBag.Categoria = _context.Categorias.ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmcategoria }).ToList();

            _context.Anuncios.Add(item);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Anuncios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Anuncios == null)
            {
                return NotFound();
            }

            var anuncio = await _context.Anuncios.FindAsync(id);
            if (anuncio == null)
            {
                return NotFound();
            }
            ViewBag.Categoria = _context.Categorias.ToList().Select(c => new SelectListItem { Value = c.id.ToString(), Text = c.nmcategoria }).ToList();
            ViewData["itemid"] = new SelectList(_context.Items, "id", "id", anuncio.itemid);
            return View(anuncio);
        }

        // POST: Anuncios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("id,vlitem,diasAluguel,iduser,itemid,categoriaid,CEP,Rua,Numero,Bairro,Cidade,Estado,Complemento,tipoid,modelo,estado,cidade,bairro,contato,NomeFantasia")] Anuncio anuncio)
        {

            if (id != anuncio.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(anuncio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnuncioExists(anuncio.id))
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
            ViewData["itemid"] = new SelectList(_context.Items, "id", "id", anuncio.itemid);
            return View(anuncio);
        }

        // GET: Anuncios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Anuncios == null)
            {
                return NotFound();
            }

            var anuncio = await _context.Anuncios
                .Include(a => a.item)
                .FirstOrDefaultAsync(m => m.id == id);
            if (anuncio == null)
            {
                return NotFound();
            }

            return View(anuncio);
        }

        // POST: Anuncios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Anuncios == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Anuncios'  is null.");
            }
            var anuncio = await _context.Anuncios.FindAsync(id);
            if (anuncio != null)
            {
                _context.Anuncios.Remove(anuncio);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnuncioExists(int id)
        {
            return (_context.Anuncios?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
