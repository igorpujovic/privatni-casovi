using System.ComponentModel.DataAnnotations;

namespace Privatni_casovi.ViewModels;

public class PrijavaViewModel
{
    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Lozinka { get; set; } = string.Empty;
}
