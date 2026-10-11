using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Learning.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private const string GenericLoginError =
            "Username atau password salah.";

        private const string InvalidEmailError =
            "Format email tidak valid. Contoh: nama@sekolah.com";

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
            // 256 = size of the Email column in the Identity schema.
            // The regex is stricter than [EmailAddress], which only
            // checks that there is one "@" not at the start or end.
            // Set the maximum email length to 25.
            [Required(ErrorMessage = "Email wajib diisi.")]
            [StringLength(25,
                ErrorMessage = "Email maksimal {1} karakter.")]
            [EmailAddress(ErrorMessage = InvalidEmailError)]
            [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                ErrorMessage = InvalidEmailError)]
            public string Email { get; set; } = string.Empty;

            // 100 = Identity's default register limit. Must not be
            // lower than the longest password your users can have.
            // Set the maximum password lenght to 10. Otherwise why would they need more than that?
            [Required(ErrorMessage = "Kata sandi wajib diisi.")]
            [StringLength(10,
                ErrorMessage = "Kata sandi maksimal {1} karakter.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            // Already signed in: skip the login page and go to the
            // user's own dashboard. Users without a supported role
            // stay on the login page (redirecting to "/" would loop,
            // since EntryController sends them back here).
            if (User.Identity?.IsAuthenticated ?? false)
            {
                if (User.IsInRole("Guru"))
                {
                    return LocalRedirect("/Teacher");
                }

                if (User.IsInRole("Murid"))
                {
                    return LocalRedirect("/Student");
                }
            }

            ReturnUrl = returnUrl ?? "/";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Same lookup PasswordSignInAsync did (by user name).
            var user = await _userManager.FindByNameAsync(Input.Email);

            // Unknown user: email is not registered.
            if (user == null)
            {
                return InvalidLogin();
            }

            // Verify the password WITHOUT issuing the auth cookie yet,
            // so the role can be checked before the user is signed in.
            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                Input.Password,
                lockoutOnFailure: false);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Akun sedang dikunci.");

                return Page();
            }

            if (!result.Succeeded)
            {
                return InvalidLogin();
            }

            string? dashboard = null;

            if (await _userManager.IsInRoleAsync(user, "Guru"))
            {
                dashboard = "/Teacher";
            }
            else if (await _userManager.IsInRoleAsync(user, "Murid"))
            {
                dashboard = "/Student";
            }

            // Unknown user: valid credentials but no supported role.
            // Never signed in, gets the same generic message.
            if (dashboard == null)
            {
                _logger.LogWarning(
                    "User {Email} has no supported role; login rejected.",
                    Input.Email);

                return InvalidLogin();
            }

            await _signInManager.SignInAsync(user, Input.RememberMe);

            _logger.LogInformation("User {Email} logged in.", Input.Email);

            return LocalRedirect(dashboard);
        }

        private PageResult InvalidLogin()
        {
            ModelState.AddModelError(string.Empty, GenericLoginError);

            return Page();
        }
    }
}