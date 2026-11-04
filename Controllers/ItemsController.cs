using aluguel.Areas.Identity.Data;
using aluguel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aluguel.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private const long MaxImageBytes = 8 * 1024 * 1024;
        private readonly ILogger<ItemsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ItemsController(ILogger<ItemsController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = context;
            _environment = webHostEnvironment;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public async Task<IActionResult> Index()
            => View(await _context.Items.Where(i => i.iduser == UserId).ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(45_000_000)]
        public async Task<IActionResult> Create(string nmitem, string dsitem, string snativo, List<IFormFile> photos)
        {
            photos ??= new List<IFormFile>();
            if (string.IsNullOrWhiteSpace(nmitem) || string.IsNullOrWhiteSpace(dsitem))
                ModelState.AddModelError("", "Preenche o nome e a descrição.");
            if (photos.Count < 1 || photos.Count > 5)
                ModelState.AddModelError("", "Seleciona entre 1 e 5 fotografias.");
            if (!ModelState.IsValid)
                return View(new aluguel.ViewModel.ItemViewModel { nmitem = nmitem, dsitem = dsitem, snativo = snativo });

            var saved = new List<string>();
            try
            {
                foreach (var photo in photos)
                    saved.Add(await SavePhotoAsync(photo));

                var item = new Item
                {
                    nmitem = nmitem.Trim(), dsitem = dsitem.Trim(), snativo = snativo == "N" ? "N" : "S",
                    dtcriacao = DateTime.UtcNow, iduser = UserId
                };
                SetImages(item, saved);
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidDataException e)
            {
                ModelState.AddModelError("", e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Não foi possível guardar item e fotografias");
                ModelState.AddModelError("", "Não foi possível guardar o item. Tenta novamente. Se persistir, contacta o suporte.");
            }
            foreach (var name in saved) DeletePhoto(name);
            return View(new aluguel.ViewModel.ItemViewModel { nmitem = nmitem, dsitem = dsitem, snativo = snativo });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == id && i.iduser == UserId);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(45_000_000)]
        public async Task<IActionResult> Edit(int id, string nmitem, string dsitem, string snativo,
            List<string> existingImages, List<IFormFile> photos)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == id && i.iduser == UserId);
            if (item == null) return NotFound();
            photos ??= new List<IFormFile>();
            existingImages ??= new List<string>();
            var original = GetImages(item);
            // Never accept an arbitrary path from the browser; only names already owned by this item.
            if (existingImages.Count != existingImages.Distinct(StringComparer.Ordinal).Count()
                || existingImages.Any(name => !original.Contains(name)))
                return BadRequest("A seleção de fotografias é inválida.");
            if (existingImages.Count + photos.Count is < 1 or > 5)
                ModelState.AddModelError("", "Mantém entre 1 e 5 fotografias.");
            if (string.IsNullOrWhiteSpace(nmitem) || string.IsNullOrWhiteSpace(dsitem))
                ModelState.AddModelError("", "Preenche o nome e a descrição.");
            if (!ModelState.IsValid) return View(item);

            var saved = new List<string>();
            try
            {
                foreach (var photo in photos)
                    saved.Add(await SavePhotoAsync(photo));
                var ordered = existingImages.Concat(saved).ToList();
                item.nmitem = nmitem.Trim();
                item.dsitem = dsitem.Trim();
                item.snativo = snativo == "N" ? "N" : "S";
                SetImages(item, ordered);
                await _context.SaveChangesAsync();
                foreach (var removed in original.Except(existingImages)) DeletePhoto(removed);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidDataException e)
            {
                ModelState.AddModelError("", e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Não foi possível editar item {Id}", id);
                ModelState.AddModelError("", "Não foi possível guardar as alterações. Tenta novamente.");
            }
            foreach (var name in saved) DeletePhoto(name);
            return View(item);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == id && i.iduser == UserId);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == id && i.iduser == UserId);
            if (item == null) return NotFound();
            var names = GetImages(item);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            foreach (var name in names) DeletePhoto(name);
            return RedirectToAction(nameof(Index));
        }

        private static List<string> GetImages(Item item)
            => new[] { item.imagem1, item.imagem2, item.imagem3, item.imagem4, item.imagem5 }
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

        private static void SetImages(Item item, IList<string> images)
        {
            item.imagem1 = images.ElementAtOrDefault(0)!;
            item.imagem2 = images.ElementAtOrDefault(1);
            item.imagem3 = images.ElementAtOrDefault(2);
            item.imagem4 = images.ElementAtOrDefault(3);
            item.imagem5 = images.ElementAtOrDefault(4);
        }

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            if (photo == null || photo.Length == 0 || photo.Length > MaxImageBytes)
                throw new InvalidDataException("Cada fotografia deve ter entre 1 byte e 8 MB.");
            byte[] header = new byte[12];
            await using (var stream = photo.OpenReadStream())
                await stream.ReadExactlyAsync(header.AsMemory(0, (int)Math.Min(photo.Length, header.Length)));
            var jpg = header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
            var png = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4e && header[3] == 0x47;
            var webp = System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            if (!jpg && !png && !webp)
                throw new InvalidDataException("Formato inválido. Utiliza JPEG, PNG ou WebP.");
            var extension = jpg ? ".jpg" : png ? ".png" : ".webp";
            var directory = Path.Combine(_environment.WebRootPath, "imagens");
            Directory.CreateDirectory(directory);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            var destination = Path.Combine(directory, fileName);
            try
            {
                await using var output = new FileStream(destination, FileMode.CreateNew);
                await photo.CopyToAsync(output);
                return fileName;
            }
            catch
            {
                if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination);
                throw;
            }
        }

        private void DeletePhoto(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name) return;
            try
            {
                var path = Path.Combine(_environment.WebRootPath, "imagens", name);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (IOException e) { _logger.LogWarning(e, "Falha ao remover fotografia {Name}", name); }
            catch (UnauthorizedAccessException e) { _logger.LogWarning(e, "Sem permissões para remover fotografia {Name}", name); }
        }
    }
}
