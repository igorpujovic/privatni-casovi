namespace Privatni_casovi.ViewModels;

/// <summary>
/// The one rule for a telefon, shared by registration and by the profile form.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately loose: a length range and the characters people actually type. There is no
/// Serbian phone-number regex here on purpose — the platform never dials the number, it only
/// shows it to an učenik who will. A stricter pattern buys nothing and eventually refuses a
/// number that works, on the one form a predavač has to get past.
/// </para>
/// <para>
/// It is a shared constant rather than two copies because the two forms write to the same
/// column: a telefon accepted at registration that the profile form then refuses to save back
/// would strand a predavač on their own profile. That was live until this issue —
/// registration used <c>[Phone]</c>, whose pattern has no <c>/</c> in it, so the very common
/// <c>064/123-4567</c> was refused. Issue 15 still owns the validation sweep; this is the one
/// case that had to move with the screen being built.
/// </para>
/// </remarks>
public static class TelefonPravilo
{
    public const int Najmanje = 6;

    public const int Najvise = 30;

    /// <summary>
    /// Digits and the separators a person types around them — and at least one digit.
    /// </summary>
    /// <remarks>
    /// The lone digit is the only thing this pattern insists on. Without it <c>------</c> is
    /// a telefon thirty characters long, and kontakt podaci are the whole point of the
    /// platform — an učenik who cannot reach the predavač has nothing.
    /// </remarks>
    public const string Sablon = @"^[0-9+/()\-. ]*[0-9][0-9+/()\-. ]*$";

    public const string PorukaSablon =
        "Telefon mora sadržati bar jednu cifru, a od ostalog samo znakove + / ( ) - . i razmak.";

    /// <summary><c>{1}</c> and <c>{2}</c> are what <c>[StringLength]</c> fills in — the
    /// limits are written once, above, and never spelled out a second time in prose.</summary>
    public const string PorukaDuzina =
        "Telefon mora imati između {2} i {1} karaktera.";
}
