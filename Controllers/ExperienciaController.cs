using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers;

/// <summary>
/// Read-only presentation of the future rental life cycle.
/// No user, payment or evidence data is collected or persisted here.
/// </summary>
public sealed class ExperienciaController : Controller
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index() => View();
}