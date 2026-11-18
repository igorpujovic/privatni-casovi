using System.ComponentModel.DataAnnotations;

namespace Privatni_casovi.Models;

/// <summary>
/// Whether a čas under an oglas is held online, uživo, or either way. The platform
/// records no place beyond this — no city, no address.
/// </summary>
/// <remarks>
/// <para>
/// An učenik searching for <see cref="Online"/> or <see cref="Uzivo"/> also sees the
/// oglasi open to <see cref="Oboje"/>; that widening lives in the search repository.
/// </para>
/// <para>
/// The <see cref="DisplayAttribute"/>s carry the diacritics the member names cannot. They
/// are what <c>Html.GetEnumSelectList</c> puts in the dropdown and what <c>DisplayFor</c>
/// renders, so every screen spells a lokacija the same way without a lookup of its own.
/// </para>
/// </remarks>
public enum Lokacija
{
    [Display(Name = "Online")]
    Online = 0,

    [Display(Name = "Uživo")]
    Uzivo = 1,

    [Display(Name = "Online i uživo")]
    Oboje = 2,
}
