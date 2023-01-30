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
        var user = new ShmearModel()
        {
            Name = name
        };
        return View(user);
    }
}