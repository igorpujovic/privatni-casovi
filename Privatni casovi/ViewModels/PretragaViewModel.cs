using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// The pretraga screen — the home page of a signed-in korisnik. Carries the filters and the
/// sort <i>in</i> from the query string and the results and the dropdown contents back out.
/// </summary>
/// <remarks>
/// <para>
/// The filters travel in the query string rather than in a posted body, which is what lets
/// the state survive both paging and re-sorting: a page link is the current query with one
/// value changed, so there is no state to lose. <see cref="RutaSaStranom"/> builds those
/// links from the same properties the form binds, so a filter cannot be added to the form and
/// forgotten in the pager.
/// </para>
/// <para>
/// A <c>null</c> filter is not a filter. All four are nullable and all four are optional; the
/// unfiltered pretraga is what an učenik lands on.
/// </para>
/// </remarks>
public class PretragaViewModel
{
    /// <summary>
    /// Cards per page. Nine fills the three-column grid exactly, and against the seeded
    /// twenty-two visible oglasi it makes three pages — enough that paging is visibly there.
    /// </summary>
    public const int VelicinaStrane = 9;

    /// <summary>Show only oglasi whose predmet belongs to this oblast.</summary>
    [Display(Name = "Oblast")]
    public int? OblastId { get; set; }

    /// <summary>
    /// Show only oglasi for this predmet. Independent of <see cref="OblastId"/> — answering
    /// both narrows to their intersection, which is empty when they disagree. There is no
    /// cascading dropdown, by decision.
    /// </summary>
    [Display(Name = "Predmet")]
    public int? PredmetId { get; set; }

    /// <summary>
    /// Show only oglasi held this way — <i>or</i> both ways. A predavač open to
    /// <see cref="Models.Lokacija.Oboje"/> answers a search for either one; that widening
    /// lives in the repository.
    /// </summary>
    [Display(Name = "Lokacija")]
    public Lokacija? Lokacija { get; set; }

    /// <summary>
    /// Show only oglasi at or below this cena časa.
    /// </summary>
    /// <remarks>
    /// The range is the one <c>OglasFormViewModel.CenaCasa</c> accepts, so every publishable
    /// cena is reachable by a filter. A value outside it is reported on the form and then
    /// ignored rather than emptying the screen — see <c>PretragaController</c>.
    /// </remarks>
    [Range(typeof(decimal), "0.01", "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Maksimalna cena mora biti pozitivan iznos.")]
    [Display(Name = "Maksimalna cena (RSD)")]
    public decimal? MaxCena { get; set; }

    /// <summary>
    /// One of the <see cref="Sortiranje"/> constants, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Nullable so that MVC does not treat an unanswered sort as a missing required field. It
    /// is not one: no sort at all is <see cref="Sortiranje.Najnoviji"/>, and a screen that
    /// silently defaults has nothing to ask the učenik for. Everything that reads this runs it
    /// through <see cref="Sortiranje.Prepoznat"/> — the controller before the view renders the
    /// dropdown, <see cref="RutaSaStranom"/> before it writes a link.
    /// </remarks>
    [Display(Name = "Sortiranje")]
    public string? SortBy { get; set; } = Sortiranje.Najnoviji;

    /// <summary>Which page of results, from 1. Clamped by the controller to a page that exists.</summary>
    public int Strana { get; set; } = 1;

    /// <summary>
    /// The oglasi on this page — <b>oglasi, not predavači</b>. A predavač with three matching
    /// oglasi is three cards here, because those are three different offers.
    /// </summary>
    [BindNever]
    [ValidateNever]
    public IReadOnlyList<OglasSaOcenom> Rezultati { get; set; } = Array.Empty<OglasSaOcenom>();

    /// <summary>
    /// Every oblast with its predmeti loaded — both filter dropdowns come out of this one
    /// list: the oblast <c>&lt;select&gt;</c> from its names, the predmet
    /// <c>&lt;select&gt;</c> from one <c>&lt;optgroup&gt;</c> per oblast.
    /// </summary>
    [BindNever]
    [ValidateNever]
    public IReadOnlyList<Oblast> Oblasti { get; set; } = Array.Empty<Oblast>();

    /// <summary>How many oglasi match the filters in total, across every page.</summary>
    [BindNever]
    public int UkupnoRezultata { get; set; }

    /// <summary>How many pages that is. Always at least 1, so an empty result still has a page.</summary>
    [BindNever]
    public int UkupnoStrana { get; set; } = 1;

    /// <summary>Whether any filter is answered — what the "Poništi filtere" button is shown for.</summary>
    public bool ImaFiltera
        => OblastId is not null || PredmetId is not null || Lokacija is not null || MaxCena is not null;

    /// <summary>
    /// The sort dropdown. Built from the <see cref="Sortiranje"/> constants rather than from
    /// literals, so a sort the repository does not implement cannot be offered.
    /// </summary>
    public static IReadOnlyList<SelectListItem> SortOpcije { get; } = new[]
    {
        new SelectListItem("Najnoviji", Sortiranje.Najnoviji),
        new SelectListItem("Cena rastuće", Sortiranje.CenaRastuce),
        new SelectListItem("Cena opadajuće", Sortiranje.CenaOpadajuce),
        new SelectListItem("Ocena opadajuće", Sortiranje.OcenaOpadajuce),
        new SelectListItem("Broj ocena opadajuće", Sortiranje.BrojOcenaOpadajuce),
    };

    /// <summary>
    /// The route values for a link to another page of <i>these</i> results: every filter and
    /// the sort, with the page number swapped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole of "filter state survives paging". Answered filters only — a
    /// <c>null</c> left out here keeps <c>?oblastId=&amp;predmetId=</c> out of the URL the
    /// učenik sees.
    /// </para>
    /// <para>
    /// The values are strings because that is what <c>asp-all-route-data</c> takes, and they
    /// are written with <see cref="CultureInfo.InvariantCulture"/> because that is what reads
    /// them back: a cena written with a Serbian decimal comma would return a hundred times
    /// larger, which is the bug <c>DecimalModelBinder</c> exists for.
    /// </para>
    /// </remarks>
    public IDictionary<string, string> RutaSaStranom(int strana)
    {
        // Clamped so that even the disabled "Prethodna" on the first page carries a page
        // number that exists — a href is worth having right whether or not it is clickable.
        var ruta = new Dictionary<string, string>
        {
            [nameof(SortBy)] = Sortiranje.Prepoznat(SortBy),
            [nameof(Strana)] = Math.Clamp(strana, 1, UkupnoStrana).ToString(CultureInfo.InvariantCulture)
        };

        if (OblastId is not null)
        {
            ruta[nameof(OblastId)] = OblastId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (PredmetId is not null)
        {
            ruta[nameof(PredmetId)] = PredmetId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (Lokacija is not null)
        {
            ruta[nameof(Lokacija)] = Lokacija.Value.ToString();
        }

        if (MaxCena is not null)
        {
            ruta[nameof(MaxCena)] = MaxCena.Value.ToString(CultureInfo.InvariantCulture);
        }

        return ruta;
    }
}
