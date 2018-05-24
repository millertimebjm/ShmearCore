using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shmear.Web.Models;

namespace Shmear.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
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
