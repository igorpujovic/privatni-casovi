using Microsoft.AspNetCore.Identity;
using Privatni_casovi.Models;

namespace Privatni_casovi.Data;

/// <summary>
/// Creates the three Identity roles if they are missing. Runs on every startup and is
/// idempotent, so a fresh database gets its roles without a manual step.
/// </summary>
/// <remarks>
/// Roles only — korisnici, oblasti, predmeti, oglasi and ocene are issue 05's seed, which
/// runs after this one and can rely on the roles existing.
/// </remarks>
public static class UlogeSeed
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var uloga in Uloge.Sve)
        {
            if (!await roleManager.RoleExistsAsync(uloga))
            {
                await roleManager.CreateAsync(new IdentityRole(uloga));
            }
        }
    }
}
