using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// Registration, prijava and odjava. Every action that is reachable without being signed in
/// is marked <c>[AllowAnonymous]</c> individually — the application otherwise requires an
/// authenticated user everywhere (see the fallback policy in <c>Program.cs</c>).
/// </summary>
/// <remarks>
/// The action names are Identity's defaults on purpose: the authentication cookie's
/// <c>LoginPath</c> and <c>AccessDeniedPath</c> already point at <c>/Account/Login</c> and
/// <c>/Account/AccessDenied</c>, so serving exactly those paths saves configuring them.
/// </remarks>
public class AccountController : Controller
{
    private readonly UserManager<Korisnik> _userManager;
    private readonly SignInManager<Korisnik> _signInManager;

    public AccountController(UserManager<Korisnik> userManager, SignInManager<Korisnik> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegistracijaViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistracijaViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var korisnik = new Korisnik
        {
            // Email doubles as the username: the brief specifies email + password login.
            UserName = model.Email,
            Email = model.Email,
            Ime = model.Ime,
            Prezime = model.Prezime,
            Telefon = model.Telefon,
            Aktivan = true
        };

        var rezultat = await _userManager.CreateAsync(korisnik, model.Lozinka);

        if (!rezultat.Succeeded)
        {
            PrikaziGreske(rezultat);
            return View(model);
        }

        // Uloga is a two-member enum, so this is total — Administrator cannot arrive here.
        var uloga = model.Uloga == UlogaIzbor.Predavac ? Uloge.Predavac : Uloge.Ucenik;
        var dodela = await _userManager.AddToRoleAsync(korisnik, uloga);

        if (!dodela.Succeeded)
        {
            // The three roles are seeded at startup, so this means the database failed
            // mid-request. Fail loudly: a korisnik with no role would pass the global
            // [Authorize] but be refused by every role-restricted screen, with nothing on
            // the form to explain why.
            throw new InvalidOperationException(
                $"Dodela uloge '{uloga}' nije uspela: {string.Join("; ", dodela.Errors.Select(g => g.Description))}");
        }

        // Registering does not sign anybody in. Prijava is its own step, and routing every
        // sign-in through PasswordSignInAsync keeps KorisnikSignInManager's block check on
        // the only path into the application.
        TempData["Poruka"] = "Registracija je uspešna. Prijavite se.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new PrijavaViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PrijavaViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rezultat = await _signInManager.PasswordSignInAsync(
            model.Email, model.Lozinka, isPersistent: false, lockoutOnFailure: false);

        if (rezultat.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, await JeBlokiran(rezultat, model)
            ? "Vaš pristup je blokiran. Obratite se administratoru."
            : "Pogrešan email ili lozinka.");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    // No [AllowAnonymous]: an anonymous request to a protected URL is sent to LoginPath, never
    // here, so this page is only ever reached by a korisnik who is signed in but lacks the
    // role. Leaving it anonymous would have widened the anonymous surface for nothing.
    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>
    /// Whether this failed prijava was a blocked korisnik rather than bad credentials.
    /// </summary>
    /// <remarks>
    /// <c>IsNotAllowed</c> on its own is not enough to say so out loud. Identity runs its
    /// pre-sign-in check <i>before</i> verifying the password, so reporting on that flag alone
    /// would tell any anonymous visitor which email addresses belong to blocked korisnici. The
    /// password is checked here so that only the owner of the account is told why.
    /// </remarks>
    private async Task<bool> JeBlokiran(Microsoft.AspNetCore.Identity.SignInResult rezultat, PrijavaViewModel model)
    {
        if (!rezultat.IsNotAllowed)
        {
            return false;
        }

        var korisnik = await _userManager.FindByEmailAsync(model.Email);
        return korisnik is not null && await _userManager.CheckPasswordAsync(korisnik, model.Lozinka);
    }

    /// <summary>
    /// Surfaces Identity's own failures on the form. The duplicate-account codes are
    /// rewritten: Identity words them in terms of a username, and this form has no username
    /// field for the korisnik to correct.
    /// </summary>
    private void PrikaziGreske(IdentityResult rezultat)
    {
        // UserName is the email, so one existing account trips both duplicate codes at once
        // and the korisnik would otherwise read the same sentence twice.
        var duplikatPrijavljen = false;

        foreach (var greska in rezultat.Errors)
        {
            if (greska.Code is nameof(IdentityErrorDescriber.DuplicateUserName)
                            or nameof(IdentityErrorDescriber.DuplicateEmail))
            {
                if (duplikatPrijavljen)
                {
                    continue;
                }

                duplikatPrijavljen = true;
                ModelState.AddModelError(
                    nameof(RegistracijaViewModel.Email),
                    "Korisnik sa ovom email adresom već postoji.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, greska.Description);
            }
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
