namespace Privatni_casovi.ViewModels;

/// <summary>
/// Turns a predavač's kontakt podaci into links a click actually acts on.
/// </summary>
/// <remarks>
/// <para>
/// The platform carries no messaging, so an email address and a telefon on screen are the
/// entire handoff from the application to the čas. Making them clickable is the difference
/// between an učenik reading a number and an učenik dialling it, and it costs two hrefs.
/// The oglas detail page and the profil predavača (issue 10) show the same two, and must
/// spell them the same way.
/// </para>
/// <para>
/// It sits beside <see cref="PrikazOcene"/> and <see cref="Mnozina"/> rather than in a folder
/// of its own: those are the same thing — how a value is spelled on screen — and this writes
/// no file and reads no database, which is what earned <c>Slike/</c> its own folder.
/// </para>
/// <para>
/// Neither argument is ever really absent. Telefon is <c>[Required]</c> on both forms that
/// write it, registration and the profile, and the email is the korisnik's <c>UserName</c>.
/// The parameters are nullable only because <c>Oglas.Predavac</c> is a nullable navigation;
/// guarding a state the application cannot produce would be dead code.
/// </para>
/// </remarks>
public static class KontaktPodaci
{
    /// <summary>The <c>mailto:</c> for an email address.</summary>
    public static string EmailHref(string? email) => $"mailto:{email}";

    /// <summary>
    /// The <c>tel:</c> for a telefon as the predavač typed it — "064 111 2201" dials as
    /// <c>tel:0641112201</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="TelefonPravilo"/> accepts the spaces, slashes and dashes people actually
    /// type, none of which belong in a URI. Only the digits are kept, plus a leading <c>+</c>
    /// where there was one — dropping that would turn an international number into a
    /// domestic one that dials somewhere else.
    /// </remarks>
    public static string TelefonHref(string? telefon)
    {
        var uneto = (telefon ?? string.Empty).Trim();
        var predznak = uneto.StartsWith('+') ? "+" : string.Empty;

        return $"tel:{predznak}{new string(uneto.Where(char.IsDigit).ToArray())}";
    }
}
