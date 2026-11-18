using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Privatni_casovi.Models;

namespace Privatni_casovi.Identity;

/// <summary>
/// Identity's <see cref="SignInManager{TUser}"/> plus one rule: a blocked korisnik
/// (<c>Aktivan == false</c>) cannot sign in.
/// </summary>
/// <remarks>
/// The check lives here rather than in <c>AccountController.Login</c> on purpose. Identity
/// funnels every sign-in path through <see cref="CanSignInAsync"/> during its pre-sign-in
/// check, so blocking is a property of the system rather than of one action that a later
/// issue could bypass — the same reasoning ADR-0001 applies to delete behaviour.
/// <para>
/// Sign-in is refused; an already-issued cookie is not revoked. An administrator blocking a
/// korisnik who is signed in at that moment (issue 13) has to update their security stamp
/// to cut the live session short.
/// </para>
/// </remarks>
public class KorisnikSignInManager : SignInManager<Korisnik>
{
    public KorisnikSignInManager(
        UserManager<Korisnik> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<Korisnik> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<Korisnik>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<Korisnik> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(Korisnik user)
        => user.Aktivan && await base.CanSignInAsync(user);
}
