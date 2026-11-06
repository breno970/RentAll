using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers;

public sealed class CookiesController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
