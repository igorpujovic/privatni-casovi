using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Slike;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// "Moj profil" — a predavač editing their own lični podaci, kontakt, biografija and
/// fotografija.
/// </summary>
/// <remarks>
/// <para>
/// There is no id anywhere on this controller, and that is the whole security story: the
/// korisnik being read and written is <c>User</c>, so unlike <see cref="MojiOglasiController"/>
/// there is nothing to check ownership of. The kontakt podaci edited here are what an učenik
/// gets on an oglas and on the public profile (issues 09 and 10) — the platform carries no
/// messaging, so this form is the only thing keeping a predavač reachable.
/// </para>
/// <para>
/// Reads and writes go through <c>UserManager</c> rather than
/// <see cref="Repositories.IKorisnikRepository"/>, which is the split that interface already
/// documents: it owns querying korisnici, Identity owns writing one. <c>UpdateAsync</c> also
/// re-runs Identity's own validation and refreshes the concurrency stamp, which a bare
/// <c>SaveChanges</c> on the entity would skip.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Predavac)]
public class ProfilController : Controller
{
    private readonly UserManager<Korisnik> _userManager;
    private readonly IWebHostEnvironment _okruzenje;

    public ProfilController(UserManager<Korisnik> userManager, IWebHostEnvironment okruzenje)
    {
        _userManager = userManager;
        _okruzenje = okruzenje;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var korisnik = await _userManager.GetUserAsync(User);

        if (korisnik is null)
        {
            return await OdjaviNepostojecegAsync();
        }

        return View(Popuni(new ProfilViewModel
        {
            Ime = korisnik.Ime,
            Prezime = korisnik.Prezime,
            Telefon = korisnik.Telefon,
            Biografija = korisnik.Biografija
        }, korisnik));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfilViewModel model)
    {
        var korisnik = await _userManager.GetUserAsync(User);

        if (korisnik is null)
        {
            return await OdjaviNepostojecegAsync();
        }

        if (!ModelState.IsValid)
        {
            // Nothing has been written and no file has been saved: an invalid form must not
            // leave an orphan in wwwroot/slike.
            return View(Popuni(model, korisnik));
        }

        korisnik.Ime = model.Ime;
        korisnik.Prezime = model.Prezime;
        korisnik.Telefon = model.Telefon;

        // A biografija nobody wrote is null, not "" — issue 10 renders "Nema biografije" off
        // exactly that, and a form posts an empty textarea as an empty string.
        korisnik.Biografija = string.IsNullOrWhiteSpace(model.Biografija) ? null : model.Biografija;

        // An empty file input means "keep the photo I have", not "remove it". The upload is
        // already known to be a jpg/jpeg/png under 2 MB — DozvoljenaFotografijaAttribute said
        // so before ModelState came back valid.
        if (model.Slika is not null)
        {
            korisnik.SlikaFajl = await Fotografija.SacuvajAsync(model.Slika, _okruzenje.WebRootPath);
        }

        var rezultat = await _userManager.UpdateAsync(korisnik);

        if (!rezultat.Succeeded)
        {
            foreach (var greska in rezultat.Errors)
            {
                ModelState.AddModelError(string.Empty, greska.Description);
            }

            return View(Popuni(model, korisnik));
        }

        TempData["Poruka"] = "Profil je sačuvan.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Fills the two display-only properties. Called before every render, including the
    /// re-render of an invalid POST: they are <c>[BindNever]</c>, so they did not travel with
    /// the request and the preview would otherwise come back empty.
    /// </summary>
    private static ProfilViewModel Popuni(ProfilViewModel model, Korisnik korisnik)
    {
        model.Email = korisnik.Email ?? string.Empty;
        model.TrenutnaSlika = korisnik.SlikaFajl;
        return model;
    }

    /// <summary>
    /// A valid cookie whose korisnik is not in the database — the account was removed out from
    /// under a live session. Sign the cookie out and send them to prijava rather than throw:
    /// there is no profile to edit, and leaving the cookie in place would repeat this on every
    /// request.
    /// </summary>
    /// <remarks>
    /// Nothing in the application deletes a korisnik (ADR-0001), so this is reachable only by
    /// hand-editing the database. It exists so the two actions can treat the korisnik as
    /// non-null without a null-forgiving operator that would be a 500 if it were ever wrong.
    /// </remarks>
    private async Task<IActionResult> OdjaviNepostojecegAsync()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
