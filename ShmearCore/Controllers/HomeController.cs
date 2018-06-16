using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shmear.Business.Services;
using Shmear.Web.Models;
using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;

namespace Shmear.Web.Controllers
{
    public class HomeController : Controller
    {
        private DbContextOptions<CardContext> _contextOptions;

        public HomeController() : base()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            optionsBuilder.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
            _contextOptions = optionsBuilder.Options;
        }

        public IActionResult Index()
        {
            try
            {
                //var gameService = new GameService(_contextOptions);
                // Initialize Database - Don't care about result
                Task.Run(() => GameService.GetOpenGame(_contextOptions));
            }
            catch(Exception ex)
            {

            }
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
