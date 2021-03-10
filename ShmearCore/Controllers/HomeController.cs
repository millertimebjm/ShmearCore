using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shmear.Business.Services;
using Shmear.Web.Models;
using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.Models;
using System;
using Shmear.EntityFramework.EntityFrameworkCore;

namespace Shmear.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbContextOptions<CardContext> _contextOptions;

        public HomeController() : base()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            //optionsBuilder.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
            optionsBuilder.UseNpgsql("Host=localhost;Database=Card.Dev;Username=postgres;Password=M8WQn8*Nz%gQEc");
            _contextOptions = optionsBuilder.Options;
        }

        public IActionResult Index()
        {
            Task.Run(() => GameService.GetOpenGame(_contextOptions));
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
