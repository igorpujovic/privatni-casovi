using System.ComponentModel.DataAnnotations;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// The role a korisnik may choose for themselves when registering.
/// </summary>
/// <remarks>
/// Administrator is deliberately absent. Because this is an enum rather than a string, a
/// crafted POST of <c>Uloga=Administrator</c> fails model binding instead of reaching the
/// role assignment — the "administrator can never self-register" rule is enforced by the
/// type, not by a check somebody could forget.
/// </remarks>
public enum UlogaIzbor
{
    Predavac,
    Ucenik
}

/// <summary>
/// The one registration form. Both predavači and učenici register through it; the role is
/// fixed here and never changes afterwards.
/// </summary>
public class RegistracijaViewModel
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100, ErrorMessage = "Ime može imati najviše {1} karaktera.")]
    [Display(Name = "Ime")]
    public string Ime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100, ErrorMessage = "Prezime može imati najviše {1} karaktera.")]
    [Display(Name = "Prezime")]
    public string Prezime { get; set; } = string.Empty;

    /// <summary>Validated by the same loose rule as the profile form — see
    /// <see cref="TelefonPravilo"/>. The two write to the same column.</summary>
    [Required(ErrorMessage = "Telefon je obavezan.")]
    [StringLength(TelefonPravilo.Najvise, MinimumLength = TelefonPravilo.Najmanje,
        ErrorMessage = TelefonPravilo.PorukaDuzina)]
    [RegularExpression(TelefonPravilo.Sablon, ErrorMessage = TelefonPravilo.PorukaSablon)]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;

    /// <summary>Also becomes the korisnik's <c>UserName</c> — the brief asks for email login.</summary>
    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu.")]
    [StringLength(256, ErrorMessage = "Email može imati najviše {1} karaktera.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.")]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Lozinka { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Lozinka), ErrorMessage = "Lozinke se ne poklapaju.")]
    [Display(Name = "Potvrda lozinke")]
    public string PotvrdaLozinke { get; set; } = string.Empty;

    /// <summary>Nullable so that submitting the form without picking a role is an error
    /// rather than a silent default to predavač.</summary>
    [Required(ErrorMessage = "Izaberite da li se registrujete kao predavač ili učenik.")]
    [Display(Name = "Registrujem se kao")]
    public UlogaIzbor? Uloga { get; set; }
}
