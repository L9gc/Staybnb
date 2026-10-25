using Microsoft.AspNetCore.Identity;
using Staybnb.Models;

namespace Staybnb.Services;

public static class RoleSeeder
{
public static async Task SeedAsync(IServiceProvider serviceProvider)
{
var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    string[] roles = { "Guest", "Host", "Admin", "SuperAdmin" };

    foreach (string role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role {role}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    string? adminEmail = configuration["SeedAdmin:Email"];
    string? adminPassword = configuration["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Configure SeedAdmin:Email and SeedAdmin:Password using user secrets.");
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Staybnb Super Administrator",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to create SuperAdmin: " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
    {
        var roleResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to assign SuperAdmin role: " +
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}

}
