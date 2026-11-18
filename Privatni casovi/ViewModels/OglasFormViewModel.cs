using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Privatni_casovi.Models;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// The one form behind objavljivanje and izmena of an oglas. Both <c>Create</c> and
/// <c>Edit</c> bind to this, and neither ever binds to <see cref="Oglas"/>.
/// </summary>
/// <remarks>
/// <para>
/// What is <i>missing</i> here is the point. <c>PredavacId</c>, <c>Aktivan</c> and
/// <c>DatumKreiranja</c> are not properties of this class, so a crafted POST carrying them
/// has nowhere to land: an oglas cannot be published in somebody else's name, and cannot be
/// resurrected past its predavač's withdrawal, by adding a field to the form. The controller
/// takes the predavač from <c>User</c> and nowhere else. That is the over-posting hole spec
/// rule 5 names, closed by the shape of the type rather than by a check.
/// </para>
/// <para>
/// The two ids are nullable so that an unanswered dropdown is a validation error rather than
/// a silent default — <c>0</c> is not a predmet, but <c>Lokacija.Online</c> <i>is</i> a
/// lokacija, and a predavač who never looked at that select would otherwise publish an oglas
/// saying something they did not say.
/// </para>
/// </remarks>
public class OglasFormViewModel
{
    /// <summary>
    /// Which oglas is being edited — <c>0</c> when publishing a new one. It is here only so
    /// the view can build the form's <c>asp-route-id</c>; the action reads the id from the
    /// route and re-checks ownership against it on every request.
    /// </summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "Izaberite predmet.")]
    [Display(Name = "Predmet")]
    public int? PredmetId { get; set; }

    [Required(ErrorMessage = "Cena časa je obavezna.")]
    [Range(typeof(decimal), "0.01", "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Cena časa mora biti pozitivan iznos.")]
    [Display(Name = "Cena časa (RSD)")]
    public decimal? CenaCasa { get; set; }

    [Required(ErrorMessage = "Izaberite gde držite časove.")]
    [Display(Name = "Lokacija")]
    public Lokacija? Lokacija { get; set; }

    [Required(ErrorMessage = "Dostupnost je obavezna.")]
    [StringLength(500, ErrorMessage = "Dostupnost može imati najviše {1} karaktera.")]
    [Display(Name = "Dostupnost")]
    public string Dostupnost { get; set; } = string.Empty;

    /// <summary>
    /// The predmet dropdown's contents: every oblast with its predmeti, which the view
    /// renders as one <c>&lt;select&gt;</c> with an <c>&lt;optgroup&gt;</c> per oblast.
    /// </summary>
    /// <remarks>
    /// Filled by the controller before every render, including the re-render of an invalid
    /// POST. <see cref="BindNeverAttribute"/> keeps it out of model binding and
    /// <see cref="ValidateNeverAttribute"/> keeps the validator from walking the seeded
    /// entity graph looking for annotations it will not find.
    /// </remarks>
    [BindNever]
    [ValidateNever]
    public IReadOnlyList<Oblast> Oblasti { get; set; } = Array.Empty<Oblast>();
}
