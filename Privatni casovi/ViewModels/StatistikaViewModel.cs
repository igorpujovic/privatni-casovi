using Privatni_casovi.Repositories;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// „Admin: statistika“ — how big the platform is, and the three rankings an administrator
/// reads it by.
/// </summary>
/// <remarks>
/// <para>
/// Every number on this screen counts active records only, which is ADR-0001's stated
/// consequence and not a filter this screen chose: a blocked korisnik and a deactivated oglas
/// are still rows in the database, and a statistika that counted them would describe a
/// platform nobody can see. The one deliberate exception is inside
/// <see cref="TopPredavaci"/> — a blocked učenik's ocena still counts towards the predavač it
/// was given to, exactly as it still counts in the prosečna ocena on that predavač's public
/// profil. See <see cref="IOcenaRepository.TopPredavaciAsync"/>.
/// </para>
/// <para>
/// No form binds to this and nothing here is posted back; it is four counts and three read-only
/// tables.
/// </para>
/// </remarks>
/// <param name="BrojVidljivihOglasa">
/// Oglasi an učenik can actually find — published, under a predavač who is not blocked. Three
/// of the twenty-five seeded oglasi are not: one its predavač withdrew, and two belonging to a
/// predavač an administrator blocked.
/// <para>
/// <b>Named for visibility and not for the flag</b>, because the two part ways and the other
/// admin screen already says so out loud: the Oglasi listing renders an oglas under a blocked
/// predavač as „Predavač blokiran“ rather than „Aktivan“, precisely so an administrator is not
/// sent hunting for an oglas they cannot find. A card headed „Aktivni oglasi“ here would count
/// twenty-two of the twenty-four rows that screen calls un-deactivated, and the two screens
/// would disagree about a word. This is the number of oglasi in the pretraga, and it says so.
/// </para>
/// </param>
/// <param name="TopPredmeti">
/// Predmeti with the most oglasi, which is supply and not demand — the table is headed „sa
/// najviše oglasa“, never „najtraženiji“. See <see cref="StavkaStatistike"/>.
/// </param>
/// <param name="TopPredavaci">
/// Ordered by broj ocena with the average as the tie-break, never by the average alone.
/// Predavači nobody has rated are absent from it rather than last.
/// </param>
public record StatistikaViewModel(
    int BrojPredavaca,
    int BrojUcenika,
    int BrojAdministratora,
    int BrojVidljivihOglasa,
    IReadOnlyList<StavkaStatistike> TopPredmeti,
    IReadOnlyList<StavkaStatistike> TopOblasti,
    IReadOnlyList<TopPredavac> TopPredavaci)
{
    /// <summary>
    /// Every korisnik who is not blocked. Derived from the three counts rather than read
    /// separately, so the total cannot disagree with the split it is printed next to.
    /// </summary>
    public int BrojKorisnika => BrojPredavaca + BrojUcenika + BrojAdministratora;
}
