using Microsoft.AspNetCore.Identity;

namespace E_Learning.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // =========================
            // CREATE ROLES
            // =========================

            string[] roles = { "Guru", "Murid" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // =========================
            // CREATE GURU
            // =========================

            var guruEmail = "guru@test.com";

            var guru = await userManager.FindByEmailAsync(guruEmail);

            if (guru == null)
            {
                guru = new IdentityUser
                {
                    UserName = guruEmail,
                    Email = guruEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    guru,
                    "Guru123!"
                );

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Failed to create Guru user."
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(guru, "Guru"))
            {
                await userManager.AddToRoleAsync(guru, "Guru");
            }

            // =========================
            // CREATE MURID
            // =========================

            var muridEmail = "murid@test.com";

            var murid = await userManager.FindByEmailAsync(muridEmail);

            if (murid == null)
            {
                murid = new IdentityUser
                {
                    UserName = muridEmail,
                    Email = muridEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    murid,
                    "Murid123!"
                );

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Failed to create Murid user."
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(murid, "Murid"))
            {
                await userManager.AddToRoleAsync(murid, "Murid");
            }
        }
    }
}