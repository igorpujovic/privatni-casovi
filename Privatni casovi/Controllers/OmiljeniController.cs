using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;

namespace Privatni_casovi.Controllers;

/// <summary>
/// "Omiljeni" — the predavači one učenik has marked to find again: the list itself, and the
/// button on the profil predavača that puts a predavač on it or takes them off.
/// </summary>
/// <remarks>
/// <para>
/// A controller of its own for the reason <see cref="OcenaController"/> is one: only an
/// učenik has omiljeni, and the profil predavača those writes are posted from is a page all
/// three uloge read. The class-level <c>[Authorize(Roles = Uloge.Ucenik)]</c> lives here so it
/// does not have to live there, and <see cref="PredavacController"/> stays what it says it is.
/// </para>
/// <para>
/// The list is <b>private to the učenik</b>, and this controller is where that is kept true:
/// there is no id on <see cref="Index"/>, so the only list anybody can ask for is their own,
/// and <see cref="IOmiljeniRepository"/> has no method that would answer "who marked this
/// predavač" for a predavač to reach. A predavač is never told how many učenici marked them,
/// on their profil or anywhere else.
/// </para>
/// <para>
/// Neither write has a GET. Both are toggles pressed from a page that is already rendered,
/// and both redirect — post-redirect-get, so a refresh after pressing does not press again.
/// The repository makes both idempotent, so a double-click adds no duplicate row and removing
/// twice is not an error; the composite primary key stays as the backstop.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Ucenik)]
public class OmiljeniController : Controller
{
    private readonly IOmiljeniRepository _omiljeni;
    private readonly IKorisnikRepository _korisnici;
    private readonly UserManager<Korisnik> _userManager;

    public OmiljeniController(
        IOmiljeniRepository omiljeni, IKorisnikRepository korisnici, UserManager<Korisnik> userManager)
    {
        _omiljeni = omiljeni;
        _korisnici = korisnici;
        _userManager = userManager;
    }

    /// <summary>Every predavač on this učenik's list, by prezime.</summary>
    /// <remarks>
    /// No id, which is the whole of this screen's security story — the učenik whose list is
    /// read is <c>User</c>. A predavač blocked since being marked is not on it: the row is
    /// left alone and <see cref="IOmiljeniRepository.ZaUcenikaAsync"/> filters them out, the
    /// same disappearance ADR-0001 gives them everywhere else.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
        // Never null: the class requires the Ucenik role, so the cookie has a korisnik.
        => View(await _omiljeni.ZaUcenikaAsync(_userManager.GetUserId(User)!));

    /// <summary>Marks one predavač. Pressed from their profil, which is where it returns to.</summary>
    /// <remarks>
    /// The predavač is resolved through <see cref="IKorisnikRepository.PredavacAsync"/> rather
    /// than trusted from the form, so a posted id belonging to an učenik, an administrator, a
    /// blocked predavač or nobody at all is a 404 before a row is written — the same answer
    /// their profil gives.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dodaj(string? predavacId)
    {
        var predavac = await _korisnici.PredavacAsync(predavacId);

        if (predavac is null)
        {
            return NotFound();
        }

        // The owner of the list is the cookie, never the request body — the form has no
        // UcenikId to post, so there is no list but your own to write to.
        await _omiljeni.DodajAsync(_userManager.GetUserId(User)!, predavac.Id);

        TempData["Poruka"] = "Predavač je dodat u omiljene.";
        return NazadNaProfil(predavac.Id);
    }

    /// <summary>Unmarks one predavač, from either of the two screens that show the mark.</summary>
    /// <param name="saListe">
    /// Which screen pressed it, and so where to land. A bool rather than a return URL on
    /// purpose: the two origins are known, and a URL taken from a form is the open redirect
    /// this does not need to have.
    /// </param>
    /// <remarks>
    /// This is the one real delete in the application — see the Scope paragraph of ADR-0001,
    /// which puts an omiljeni row outside the soft-delete rule because a bookmark nobody else
    /// can see has nothing to preserve.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ukloni(string? predavacId, bool saListe)
    {
        var predavac = await _korisnici.PredavacAsync(predavacId);

        if (predavac is null)
        {
            return NotFound();
        }

        await _omiljeni.UkloniAsync(_userManager.GetUserId(User)!, predavac.Id);

        TempData["Poruka"] = "Predavač je uklonjen iz omiljenih.";
        return saListe ? RedirectToAction(nameof(Index)) : NazadNaProfil(predavac.Id);
    }

    /// <summary>The profil the button was pressed on.</summary>
    private IActionResult NazadNaProfil(string predavacId)
        => RedirectToAction(nameof(PredavacController.Profil), "Predavac", new { id = predavacId });
}
