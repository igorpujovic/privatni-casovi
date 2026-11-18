using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// An učenik rating a predavač — the one write behind the form on the profil predavača.
/// </summary>
/// <remarks>
/// <para>
/// A controller of its own rather than a second action on <see cref="PredavacController"/>,
/// because the role requirement is the whole point: only an učenik may rate, and the profil
/// predavača is a page all three uloge read. Putting the class-level
/// <c>[Authorize(Roles = Uloge.Ucenik)]</c> here keeps it off that page, and leaves
/// <see cref="PredavacController"/> what it says it is — a screen that writes nothing.
/// <see cref="OmiljeniController"/> is the same shape for the same reason.
/// </para>
/// <para>
/// There is no GET. The form is markup on the profil predavača, which already has the
/// predavač loaded and the učenik's existing score in hand, so a separate rating page would
/// be a second round trip to render one <c>&lt;select&gt;</c>. Every path out of here is a
/// redirect back to that profil: post-redirect-get, so a refresh after rating does not re-post
/// the score.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Ucenik)]
public class OcenaController : Controller
{
    private readonly IOcenaRepository _ocene;
    private readonly IKorisnikRepository _korisnici;
    private readonly UserManager<Korisnik> _userManager;

    public OcenaController(
        IOcenaRepository ocene, IKorisnikRepository korisnici, UserManager<Korisnik> userManager)
    {
        _ocene = ocene;
        _korisnici = korisnici;
        _userManager = userManager;
    }

    /// <summary>
    /// Records this učenik's score for one predavač — inserting the first time, and editing
    /// that same row every time after.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IOcenaRepository.PostojecaAsync"/> is asked before anything is staged, and
    /// that lookup is what keeps the unique index on (UcenikId, PredavacId) from ever being
    /// the thing that says no: a second rating is an edit, so the učenik gets "Ocena je
    /// izmenjena" rather than a <c>DbUpdateException</c>. The index stays as the backstop.
    /// </para>
    /// <para>
    /// The predavač is resolved through
    /// <see cref="IKorisnikRepository.PredavacAsync"/> rather than trusted from the form, so a
    /// posted id belonging to an učenik, an administrator, a blocked predavač or nobody at all
    /// is a 404 before a row is touched — the same answer their profil gives.
    /// </para>
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Oceni(OcenaFormViewModel model)
    {
        var predavac = await _korisnici.PredavacAsync(model.PredavacId);

        if (predavac is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            // The form is a closed <select> on a page this controller does not own, so the
            // errors are carried back as a message rather than as ModelState: re-rendering
            // the profil from here would mean composing all four of its reads a second time,
            // for a form whose only reachable error is submitting the placeholder option.
            //
            // The text is decided from the bound model rather than read out of ModelState.
            // A Vrednost that is not a number at all — Vrednost=abc from a crafted POST —
            // fails in the model binder before [Required] or [Range] runs, so ModelState holds
            // the binder's own "mora biti broj" (Serbian since issue 15's PorukeVezivanja, but
            // still the binder's). That sentence is about a field this reader never typed into:
            // the form is a closed <select>. Unbound and unchosen are the same thing to them,
            // and these two sentences are the ones that fit the page they are shown on.
            TempData["Greska"] = model.Vrednost is null
                ? OcenaFormViewModel.NijeIzabrana
                : OcenaFormViewModel.VanOpsega;

            return NazadNaProfil(predavac.Id);
        }

        var ucenikId = _userManager.GetUserId(User)!;
        var postojeca = await _ocene.PostojecaAsync(ucenikId, predavac.Id);

        if (postojeca is null)
        {
            _ocene.Add(new Ocena
            {
                // The author is the cookie, never the request body — the form has no
                // UcenikId to post.
                UcenikId = ucenikId,
                PredavacId = predavac.Id,
                Vrednost = model.Vrednost!.Value
            });
        }
        else
        {
            // Vrednost is the only field an ocena has that anybody may change: the pair it
            // belongs to is what identified this row in the first place.
            postojeca.Vrednost = model.Vrednost!.Value;
            _ocene.Update(postojeca);
        }

        try
        {
            await _ocene.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The lookup above is what normally keeps the unique index quiet, but it cannot
            // cover two requests racing each other — a double-clicked button, where both
            // find no ocena and both insert. The index refuses the loser, and the point of
            // catching it is the one in the issue: a message rather than an exception.
            //
            // Both requests carry the same score, which is the only way this happens in
            // practice, so the redirect lands on a profil showing the ocena that did save.
            TempData["Poruka"] = "Ocena je sačuvana.";
            return NazadNaProfil(predavac.Id);
        }

        // Nothing recomputes the prosečna ocena, because nothing stores it — the redirected
        // GET averages the rows as they now are and the number has already moved.
        TempData["Poruka"] = postojeca is null ? "Ocena je sačuvana." : "Ocena je izmenjena.";
        return NazadNaProfil(predavac.Id);
    }

    /// <summary>The profil the form was posted from — the only destination this controller has.</summary>
    private IActionResult NazadNaProfil(string predavacId)
        => RedirectToAction(nameof(PredavacController.Profil), "Predavac", new { id = predavacId });

}
