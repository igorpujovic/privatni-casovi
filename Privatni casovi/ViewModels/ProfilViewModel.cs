using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Privatni_casovi.Slike;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// "Moj profil" — the form a predavač edits their own lični podaci, kontakt, biografija and
/// fotografija on.
/// </summary>
/// <remarks>
/// <para>
/// As with <see cref="OglasFormViewModel"/>, what is missing is the point. There is no
/// <c>Id</c>, no <c>Email</c> to write, no <c>Aktivan</c> and no <c>SlikaFajl</c>: the
/// korisnik being edited is the one in the cookie, the email is the login name, blocking is
/// an administrator's call (issue 13), and the stored photo name is written from a file this
/// form uploaded — never from a name it posted. Without that last one a crafted POST could
/// point a profile at any file under <c>wwwroot</c>.
/// </para>
/// <para>
/// The two display-only properties carry values <i>out</i> to the view and are
/// <see cref="BindNeverAttribute"/> so nothing can carry them back in.
/// </para>
/// </remarks>
public class ProfilViewModel
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100, ErrorMessage = "Ime može imati najviše {1} karaktera.")]
    [Display(Name = "Ime")]
    public string Ime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100, ErrorMessage = "Prezime može imati najviše {1} karaktera.")]
    [Display(Name = "Prezime")]
    public string Prezime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon je obavezan.")]
    [StringLength(TelefonPravilo.Najvise, MinimumLength = TelefonPravilo.Najmanje,
        ErrorMessage = TelefonPravilo.PorukaDuzina)]
    [RegularExpression(TelefonPravilo.Sablon, ErrorMessage = TelefonPravilo.PorukaSablon)]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;

    /// <summary>Optional — a predavač with nothing written here has no biografija, not "".</summary>
    [StringLength(1000, ErrorMessage = "Biografija može imati najviše {1} karaktera.")]
    [Display(Name = "Biografija")]
    public string? Biografija { get; set; }

    /// <summary>
    /// A new photo, or <c>null</c> when the predavač is only editing text. Leaving the file
    /// input empty keeps the photo they already have.
    /// </summary>
    [DozvoljenaFotografija]
    [Display(Name = "Nova fotografija")]
    public IFormFile? Slika { get; set; }

    /// <summary>The photo currently on file, for the preview. Display only.</summary>
    [BindNever]
    public string? TrenutnaSlika { get; set; }

    /// <summary>
    /// The login email, shown so a predavač can read it. Display only: it doubles as the
    /// Identity <c>UserName</c>, and changing a login from a profile form is nowhere in the
    /// brief.
    /// </summary>
    [BindNever]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Server-side validation of an uploaded fotografija: extension and size, both refused as a
/// message on the form rather than as an exception.
/// </summary>
/// <remarks>
/// An empty file input binds to <c>null</c> and is valid — not uploading a photo is the
/// normal case. A zero-byte file is a different thing and is refused: silently ignoring it
/// would leave a predavač looking at their old photo with a success message on screen.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DozvoljenaFotografijaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not IFormFile upload)
        {
            return ValidationResult.Success;
        }

        if (upload.Length == 0)
        {
            return new ValidationResult("Izabrana datoteka je prazna.");
        }

        if (!Fotografija.JeDozvoljenaEkstenzija(upload.FileName))
        {
            return new ValidationResult(
                $"Fotografija mora biti {Fotografija.DozvoljeniTipovi} datoteka.");
        }

        // "Under 2 MB" is implemented as "at most 2 MB" — one byte apart, and it is the
        // reading the form's own "najviše 2 MB" gives a predavač.
        if (upload.Length > Fotografija.NajvecaVelicina)
        {
            return new ValidationResult(
                $"Fotografija ne sme biti veća od {Fotografija.NajvecaVelicinaMb} MB.");
        }

        return ValidationResult.Success;
    }
}
