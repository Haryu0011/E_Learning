using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace E_Learning.Controllers
{
    public class HomeController : Controller
    {
        // Intentionally remove the index() on this controller
        // Since we already have the index() for each roles
        // controlled by the EntryController, thus we no longer need
        // default home default page ("the one with `welcome` message).
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
