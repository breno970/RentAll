using aluguel.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace aluguel.Controllers;

/// <summary>
/// Keeps existing Razor image paths (/imagens/{name}) compatible with remote storage.
/// Physical assets in wwwroot/imagens are still handled by UseStaticFiles first.
/// </summary>
public class ImagesController : Controller
{
    private readonly IImageStorage _storage;
    public ImagesController(IImageStorage storage) => _storage = storage;

    [HttpGet("/imagens/{**key}")]
    public IActionResult Show(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            !Regex.IsMatch(key, @"^[A-Za-z0-9_-]+/[a-f0-9]{32}\.(jpg|png|webp)$"))
            return NotFound();
        return Redirect(_storage.PublicUrl(key));
    }
}
