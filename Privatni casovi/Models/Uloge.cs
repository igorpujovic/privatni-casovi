namespace Privatni_casovi.Models;

/// <summary>
/// The three Identity roles. Every korisnik holds exactly one of them, fixed at
/// registration and never changed.
/// </summary>
/// <remarks>
/// These are <c>const</c> so they can be used in attributes — <c>[Authorize(Roles =
/// Uloge.Predavac)]</c> — which is the whole reason the role names are not just literals.
/// </remarks>
public static class Uloge
{
    public const string Predavac = "Predavac";

    public const string Ucenik = "Ucenik";

    /// <summary>Seeded, never self-registered — see <c>RegistracijaViewModel.Uloga</c>.</summary>
    public const string Administrator = "Administrator";

    public static readonly string[] Sve = { Predavac, Ucenik, Administrator };
}
