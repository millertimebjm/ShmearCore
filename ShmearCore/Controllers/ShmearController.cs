using Microsoft.AspNetCore.Mvc;
using Shmear.Web.Models;

namespace Shmear.Web.Controllers
{
    public class ShmearController : Controller
    {
        public IActionResult Index(string name)
        {
            var user = new ShmearModel()
            {
                Name = name
            };
            return View(user);
        }
    }
}