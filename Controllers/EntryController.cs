using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Learning.Controllers
{
    [AllowAnonymous]
    public class EntryController : Controller
    {
        public IActionResult Index()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToPage(
                    "/Account/Login",
                    new
                    {
                        area = "Identity"
                    });
            }

            if (User.IsInRole("Guru"))
            {
                return RedirectToAction(
                    "Index",
                    "Teacher");
            }

            if (User.IsInRole("Murid"))
            {
                return RedirectToAction(
                    "Index",
                    "Student");
            }

            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }
    }
}