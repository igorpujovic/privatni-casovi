using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// The pretraga — the home page of a signed-in korisnik, and the screen every other one is
/// reached from. Open to all three uloge: an administrator and a predavač see the same
/// listing an učenik does.
/// </summary>
/// <remarks>
/// There is no <c>[Authorize]</c> here because there does not need to be: the fallback policy
/// in <c>Program.cs</c> already requires a signed-in korisnik everywhere that does not say
/// <c>[AllowAnonymous]</c>, and oglasi are not visible to anonymous visitors.
/// </remarks>
public class PretragaController : Controller
{
    private readonly IOglasRepository _oglasi;
    private readonly IPredmetRepository _predmeti;

    public PretragaController(IOglasRepository oglasi, IPredmetRepository predmeti)
    {
        _oglasi = oglasi;
        _predmeti = predmeti;
    }

    /// <summary>
    /// Filters, sorts and pages the oglasi. Every parameter arrives in the query string, so
    /// the state of the screen is its URL — which is what makes it survive paging, re-sorting
    /// and the back button alike.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one query is <see cref="IOglasRepository.PretragaAsync"/>: the filtering, the
    /// sorting and the ADR-0001 <c>Aktivan</c> rule are all its business, and this action
    /// composes no LINQ of its own.
    /// </para>
    /// <para>
    /// Paging is the exception, and it is deliberately done here over the returned list rather
    /// than pushed into the repository. The pretraga signature is fixed by the spec at four
    /// filters and a sort, and the platform's whole inventory is a couple of dozen oglasi —
    /// buying a <c>SKIP … FETCH</c> at the price of a second repository method and a second
    /// round trip for the count would be paying for a scale this application does not have.
    /// If the catalogue ever grows, the fix is a page and a size on that method, and this
    /// action shrinks.
    /// </para>
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(PretragaViewModel model)
    {
        // A sort nobody offers, hand-typed into the URL, shows results rather than an error.
        model.SortBy = Sortiranje.Prepoznat(model.SortBy);

        // The select tag helper reads what was *posted* before it reads the model, so without
        // this the dropdown would render with nothing selected at all on a value the model
        // just corrected. Dropping the entry is what lets the corrected value through.
        ModelState.Remove(nameof(PretragaViewModel.SortBy));

        // A cena that is not a positive amount is reported next to the field and then dropped
        // from the query. Refusing to search at all would answer a mistyped filter with an
        // empty screen, which reads as "there are no oglasi" rather than "fix this number".
        //
        // This asks after that one field rather than after ModelState as a whole: a lokacija
        // that failed to parse is no reason to throw away a cena the učenik typed correctly,
        // and dropping it silently is worse than not filtering — the results would look
        // filtered and not be.
        if (ModelState[nameof(PretragaViewModel.MaxCena)]?.Errors.Count > 0)
        {
            model.MaxCena = null;
        }

        var sviRezultati = await _oglasi.PretragaAsync(
            model.OblastId, model.PredmetId, model.Lokacija, model.MaxCena, model.SortBy);

        model.UkupnoRezultata = sviRezultati.Count;
        model.UkupnoStrana = Math.Max(
            1, (int)Math.Ceiling(sviRezultati.Count / (double)PretragaViewModel.VelicinaStrane));

        // Clamped rather than 404'd: a page number left over from a wider filter is a normal
        // thing to arrive with, and the answer to it is the last page that does exist.
        model.Strana = Math.Clamp(model.Strana, 1, model.UkupnoStrana);

        model.Rezultati = sviRezultati
            .Skip((model.Strana - 1) * PretragaViewModel.VelicinaStrane)
            .Take(PretragaViewModel.VelicinaStrane)
            .ToList();

        // Both dropdowns come from this one read, and it happens on every render because the
        // contents of a <select> do not travel with the request that submitted it.
        model.Oblasti = await _predmeti.OblastiSaPredmetimaAsync();

        return View(model);
    }
}
