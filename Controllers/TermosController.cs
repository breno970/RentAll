using Microsoft.AspNetCore.Mvc;

namespace aluguel.Controllers
{
    public class TermosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
