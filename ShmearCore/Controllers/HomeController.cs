using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shmear.Business.Services;
using Shmear.Web.Models;

namespace Shmear.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Initialize Database - Don't care about result
            Task.Run(() => GameService.GetOpenGame());

            return View();
        }

        public ActionResult Start(string name)
        {
            return RedirectToAction("Index", "Shmear", new { name });
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
