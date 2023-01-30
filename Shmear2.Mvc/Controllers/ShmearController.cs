using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shmear2.Mvc.Models;
using System.Security.Principal;
using System.Security.Claims;

namespace Shmear2.Mvc.Controllers;

public class ShmearController : Controller
{
    private readonly ILogger<ShmearController> _logger;

    public ShmearController(ILogger<ShmearController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return RedirectToAction("Index", "Home");
        }
        // HttpContext.User = new GenericPrincipal(
        //     new GenericIdentity(name), null);
        var identity = new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, name)
        });
        HttpContext.User = new ClaimsPrincipal(identity);
        var user = new ShmearModel()
        {
            Name = name
        };
        return View(user);
    }
}