using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Learning.Controllers
{
    // ============================================================
    // ENTRY CONTROLLER
    // ============================================================
    //
    // Acts as the main entry point for the application.
    //
    // The controller determines where a user should be redirected
    // based on:
    //
    //   1. Whether the user is authenticated
    //   2. Whether the user has the "Guru" (Teacher) role
    //   3. Whether the user has the "Murid" (Student) role
    //
    // This controller does not contain application business logic.
    // Its main purpose is to route users to the appropriate
    // starting page.
    //
    // AllowAnonymous is required because unauthenticated users
    // must be able to access this controller before logging in.
    //
    [AllowAnonymous]
    public class EntryController : Controller
    {
        // ========================================================
        // APPLICATION ENTRY POINT
        // ========================================================
        //
        //  TL;DR : Routing User to their respective role page if authenticated
        //
        // Determines the appropriate destination for the current
        // user.
        //
        // Intended flows:
        //  Case 1 :
        //      Not authenticated
        //              ↓
        //      Identity Login
        //
        //  Case 2 : 
        //      Authenticated + Guru
        //              ↓
        //      Teacher Dashboard
        //
        //  Case 3 : 
        //      Authenticated + Murid
        //              ↓
        //      Student Dashboard
        //
        [HttpGet]
        public IActionResult Index()
        {
            // ----------------------------------------------------
            // 1. CHECK AUTHENTICATION
            // ----------------------------------------------------
            //
            // If the user is not logged in, redirect them to
            // ASP.NET Core Identity's Login page.
            //
            // The null-conditional check prevents problems if
            // User.Identity is unavailable.
            //
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToPage(
                    "/Account/Login",
                    new
                    {
                        area = "Identity"
                    });
            }


            // ----------------------------------------------------
            // 2. CHECK TEACHER ROLE
            // ----------------------------------------------------
            //
            // Teachers with the "Guru" role are sent to the
            // TeacherController's Index action.
            //
            if (User.IsInRole("Guru"))
            {
                return RedirectToAction(
                    "Index",
                    "Teacher");
            }


            // ----------------------------------------------------
            // 3. CHECK STUDENT ROLE
            // ----------------------------------------------------
            //
            // Students with the "Murid" role are sent to the
            // StudentController's Index action.
            //
            if (User.IsInRole("Murid"))
            {
                return RedirectToAction(
                    "Index",
                    "Student");
            }


            // ----------------------------------------------------
            // 4. FALLBACK
            // ----------------------------------------------------
            //
            // If the user is authenticated but does not have one
            // of the expected application roles, send them back
            // to the Identity login page.
            //
            // This prevents the user from reaching an application
            // area for which no supported role has been assigned.
            //
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }
    }
}