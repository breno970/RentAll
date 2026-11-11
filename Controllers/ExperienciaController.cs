using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers;

/// <summary>Public information and a non-persistent rental journey walkthrough.</summary>
public sealed class ExperienciaController : Controller
{
    // Preserve existing public links to /Experiencia.
    [HttpGet]
    public IActionResult Index() => RedirectToActionPermanent(nameof(ComoFunciona));

    [HttpGet("/como-funciona")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ComoFunciona() => View("Index");
}