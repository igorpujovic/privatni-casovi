using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// The administrator's screens: every korisnik with the uloga they hold, blocked or not, and
/// every oglas on the platform, deactivated or not — with the flag on each one flippable from
/// the listing it appears in — plus the statistika read off what those flags leave visible.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no delete action on this controller, and there is nowhere else one could
/// live.</b> Blocking a korisnik and deactivating an oglas are the only removals the
/// application has (ADR-0001): a hard delete would cascade away ocene and silently move the
/// prosečna ocena shown on unrelated predavači's public profiles, so the two listings here
/// show every row that ever existed and offer a flag instead of a delete button.
/// </para>
/// <para>
/// These are the only two reads in the application that see deactivated rows belonging to
/// somebody else, and they say so in their names —
/// <see cref="IKorisnikRepository.SviUkljucujuciNeaktivneAsync"/> and
/// <see cref="IOglasRepository.SviUkljucujuciNeaktivneAsync"/>. Every read of a korisnik or an
/// oglas goes through a method that filters on <c>Aktivan</c>, which is what makes one flip
/// here reach all of them at once: a blocked predavač's oglasi leave the pretraga without any
/// of them being touched, their profil 404s, and they drop off the omiljeni lists that name
/// them.
/// </para>
/// <para>
/// <b>Ocene are the carve-out.</b> <see cref="IOcenaRepository"/> filters on nothing, so a
/// blocked učenik's ocena goes on counting in the prosečna ocena of every predavač they rated.
/// That is deliberate and it is ADR-0001's own reasoning read the other way: the ADR rejects
/// hard deletes precisely because removing an učenik would "silently move the prosečna ocena
/// shown on unrelated predavači's public profiles", and hiding their ocena on block would move
/// those averages exactly the same way. Blocking is about what a korisnik can do and what of
/// theirs is on display, not about retracting a rating other people's profiles already carry.
/// </para>
/// <para>
/// Both listings are the whole table with no paging. The seeded platform is twenty-one
/// korisnici and twenty-five oglasi and the brief asks for no more; paging a table this size
/// would be machinery with nothing to do.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Administrator)]
public class AdminController : Controller
{
    /// <summary>
    /// How many rows each ranking on the statistika screen shows. Five, against a seeded
    /// fifteen predmeti and five oblasti: a „top“ table long enough to hold every oblast and
    /// short enough that the predmeti on it are actually the top of something.
    /// </summary>
    private const int VelicinaRangListe = 5;

    private readonly IKorisnikRepository _korisnici;
    private readonly IOglasRepository _oglasi;
    private readonly IOcenaRepository _ocene;
    private readonly UserManager<Korisnik> _userManager;

    public AdminController(
        IKorisnikRepository korisnici,
        IOglasRepository oglasi,
        IOcenaRepository ocene,
        UserManager<Korisnik> userManager)
    {
        _korisnici = korisnici;
        _oglasi = oglasi;
        _ocene = ocene;
        _userManager = userManager;
    }

    /// <summary>/Admin on its own is the korisnici listing, not a 404.</summary>
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Korisnici));

    /// <summary>
    /// Every korisnik with their uloga and whether they are blocked, by prezime.
    /// </summary>
    /// <remarks>
    /// Two reads rather than a join in the repository: the korisnici, and one dictionary of
    /// role memberships zipped onto them here. Keeping the uloga off <see cref="Korisnik"/> is
    /// Identity's design and not ours — the role is not a column on the korisnik, and adding
    /// one would be a second source of truth for the only thing
    /// <c>[Authorize(Roles = ...)]</c> actually reads.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Korisnici()
    {
        var korisnici = await _korisnici.SviUkljucujuciNeaktivneAsync();
        var uloge = await _korisnici.UlogePoKorisnikuAsync();
        var ja = _userManager.GetUserId(User);

        return View(korisnici
            .Select(k => new AdminKorisnikViewModel(
                k,
                uloge.TryGetValue(k.Id, out var uloga) ? uloga : string.Empty,
                JeJa: k.Id == ja))
            .ToList());
    }

    /// <summary>
    /// Blocks a korisnik: they can no longer sign in, and if they are a predavač their oglasi
    /// leave the pretraga. Nothing is deleted.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Blokiraj(string? id)
        => PostaviAktivnostKorisnikaAsync(id, aktivan: false, "Korisnik je blokiran.");

    /// <summary>Lets a blocked korisnik back in, with everything of theirs that was hidden.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Odblokiraj(string? id)
        => PostaviAktivnostKorisnikaAsync(id, aktivan: true, "Korisnik je odblokiran.");

    /// <summary>
    /// Every oglas on the platform, deactivated ones and blocked predavači's included, newest
    /// first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Oglasi()
        => View(await _oglasi.SviUkljucujuciNeaktivneAsync());

    /// <summary>Takes one oglas out of the pretraga, over its predavač's head. Nothing is deleted.</summary>
    /// <remarks>
    /// The same flag its own predavač flips from "Moji oglasi", deliberately: an oglas an
    /// administrator deactivated is a row its owner can reactivate, and there is no separate
    /// "deactivated by an administrator" state for them to be unable to undo. The brief asks an
    /// administrator to deactivate oglasi, not to win an argument with a predavač about one,
    /// and a second flag would be a second rule for every read to filter on.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deaktiviraj(int id)
        => PostaviAktivnostOglasaAsync(id, aktivan: false, "Oglas je deaktiviran.");

    /// <summary>Puts a deactivated oglas back into the pretraga.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Aktiviraj(int id)
        => PostaviAktivnostOglasaAsync(id, aktivan: true, "Oglas je ponovo aktiviran.");

    /// <summary>
    /// How big the platform is and what is busiest on it: korisnici by uloga, visible oglasi,
    /// and the three rankings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four reads and no logic. Each ranking is one <c>GROUP BY</c> in the repository that
    /// owns the rows it groups — the two „sa najviše oglasa“ tables over oglasi, the predavači
    /// over ocene — because a controller that assembled them itself would be composing queries
    /// and would have to remember the <c>Aktivan</c> filter of ADR-0001 at four more call
    /// sites.
    /// </para>
    /// <para>
    /// The counts are read out of the dictionary with <c>GetValueOrDefault</c> rather than
    /// indexed: a <c>GROUP BY</c> has no row for an uloga nobody active holds, and blocking
    /// the last administrator is enough to produce one — this screen should still render,
    /// showing the zero it means.
    /// </para>
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Statistika()
    {
        var poUlozi = await _korisnici.BrojAktivnihPoUloziAsync();

        return View(new StatistikaViewModel(
            BrojPredavaca: poUlozi.GetValueOrDefault(Uloge.Predavac),
            BrojUcenika: poUlozi.GetValueOrDefault(Uloge.Ucenik),
            BrojAdministratora: poUlozi.GetValueOrDefault(Uloge.Administrator),
            BrojVidljivihOglasa: await _oglasi.BrojVidljivihAsync(),
            TopPredmeti: await _oglasi.TopPredmetiAsync(VelicinaRangListe),
            TopOblasti: await _oglasi.TopOblastiAsync(VelicinaRangListe),
            TopPredavaci: await _ocene.TopPredavaciAsync(VelicinaRangListe)));
    }

    /// <summary>
    /// Blocks or unblocks, having first refused the one korisnik an administrator may not
    /// block: themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The self-block is refused <i>here</i> and not only by leaving the button off their own
    /// row. The row renders without a button so the screen does not offer something it will
    /// not do, but the action is what enforces it — a hand-crafted POST of their own id
    /// reaches this method and nothing else stands between it and the flag. An administrator
    /// who blocked themselves could not sign back in to undo it, and with one seeded
    /// administrator that is the platform locked with nobody able to open it.
    /// </para>
    /// <para>
    /// Blocking a <i>different</i> administrator is allowed. The rule the brief states is
    /// about not being able to lock yourself out, and an administrator locked out by a
    /// colleague still has a colleague who can let them back in.
    /// </para>
    /// </remarks>
    private async Task<IActionResult> PostaviAktivnostKorisnikaAsync(string? id, bool aktivan, string poruka)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (!aktivan && id == _userManager.GetUserId(User))
        {
            TempData["Greska"] = "Ne možete blokirati sami sebe.";
            return RedirectToAction(nameof(Korisnici));
        }

        if (!await _korisnici.PostaviAktivnostAsync(id, aktivan))
        {
            return NotFound();
        }

        await PrekiniSesijuAsync(id, aktivan);

        TempData["Poruka"] = poruka;
        return RedirectToAction(nameof(Korisnici));
    }

    /// <summary>
    /// Cuts short the session of a korisnik who was signed in at the moment they were blocked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>KorisnikSignInManager</c> refuses a blocked korisnik at sign-in, which does nothing
    /// about a cookie that was already issued: without this, blocking somebody mid-session
    /// leaves them browsing until their cookie expires. Changing the security stamp is what
    /// invalidates that cookie — Identity revalidates a signed-in principal against the stamp
    /// periodically and signs out one whose stamp no longer matches.
    /// </para>
    /// <para>
    /// "Periodically" is Identity's default half-hour, left at the default on purpose: the
    /// alternative is revalidating on every request, which is a database round trip on every
    /// request of every korisnik in order to make one administrator's action land sooner. The
    /// sign-in refusal is immediate and it is the one the brief asks for; this is the cleanup
    /// behind it.
    /// </para>
    /// <para>
    /// Unblocking does not touch the stamp — there is no stale session to end. A korisnik who
    /// is not found here is not an error either: the caller has already written the flag
    /// against that same id and been answered, so the only way to arrive at a missing row is a
    /// deletion this application cannot perform.
    /// </para>
    /// </remarks>
    private async Task PrekiniSesijuAsync(string id, bool aktivan)
    {
        if (aktivan)
        {
            return;
        }

        if (await _userManager.FindByIdAsync(id) is { } korisnik)
        {
            await _userManager.UpdateSecurityStampAsync(korisnik);
        }
    }

    /// <summary>
    /// Flips one oglas's flag and lands back on the listing. An id belonging to no oglas is a
    /// 404 rather than a redirect with a message about something that did not happen.
    /// </summary>
    private async Task<IActionResult> PostaviAktivnostOglasaAsync(int id, bool aktivan, string poruka)
    {
        if (!await (aktivan ? _oglasi.AktivirajAsync(id) : _oglasi.DeaktivirajAsync(id)))
        {
            return NotFound();
        }

        TempData["Poruka"] = poruka;
        return RedirectToAction(nameof(Oglasi));
    }
}
