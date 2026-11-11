using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers;

public sealed class LegalController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
