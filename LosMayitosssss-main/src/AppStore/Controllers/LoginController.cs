using Microsoft.AspNetCore.Mvc;

namespace AppStore.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 