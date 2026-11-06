using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers;

public sealed class PlanosController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
