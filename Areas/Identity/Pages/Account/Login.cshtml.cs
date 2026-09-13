using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Learning.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = "/";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", Input.Email);

                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, "Guru"))
                    {
                        return LocalRedirect("/Teacher");
                    }

                    if (await _userManager.IsInRoleAsync(user, "Murid"))
                    {
                        return LocalRedirect("/Student");
                    }
                }

                return LocalRedirect("/Identity/Account/Login");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Akun sedang dikunci.");

                return Page();
            }

            ModelState.AddModelError(
                string.Empty,
                "Username atau password salah.");

            return Page();
        }
    }
}