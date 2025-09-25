using Microsoft.AspNetCore.Mvc;

namespace AppStore.Controllers
{
    public class UsuariosController : Controller
    {
        public IActionResult Registrar()
        {
            return View();
        }

        public IActionResult Modificar()
        {
            return View();
        }

        public IActionResult Eliminar()
        {
            return View();
        }
    }
} 