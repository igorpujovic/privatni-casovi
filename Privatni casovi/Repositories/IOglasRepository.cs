using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// Reads and writes over oglasi. Every read here hides oglasi that were deactivated
/// <i>and</i> oglasi whose predavač was blocked, except the two methods that say otherwise
/// in their name.
/// </summary>
public interface IOglasRepository : IRepository<Oglas>
{
    /// <summary>
    /// The pretraga behind the home page. Each parameter is a filter that a <c>null</c>
    /// switches off, so no argument at all lists every visible oglas.
    /// </summary>
    /// <remarks>
    /// Results are oglasi, not predavači: a predavač with three matching oglasi comes back
    /// three times, because those are three different offers.
    /// </remarks>
    /// <param name="lokacija">
    /// Matches this value <b>or</b> <see cref="Lokacija.Oboje"/> — a predavač who teaches
    /// both ways answers a search for either one.
    /// </param>
    /// <param name="sortBy">One of the <see cref="Sortiranje"/> constants; anything else sorts newest-first.</param>
    Task<IReadOnlyList<OglasSaOcenom>> PretragaAsync(
        int? oblastId, int? predmetId, Lokacija? lokacija, decimal? maxCena, string? sortBy);

    /// <summary>
    /// One visible oglas with everything the detail page shows. <c>null</c> if it does not
    /// exist, was deactivated, or belongs to a blocked predavač.
    /// </summary>
    Task<OglasSaOcenom?> AktivanAsync(int id);

    /// <summary>
    /// The oglasi of one predavač that an učenik can see, newest first — the listing on their
    /// public profil. Each arrives with its <c>Predmet</c> and that predmet's <c>Oblast</c>.
    /// </summary>
    /// <remarks>
    /// Plain oglasi rather than <see cref="OglasSaOcenom"/>, which is the one read here that
    /// does not want a standing attached: every oglas in this list belongs to the same
    /// predavač, so the ocene would be the same two numbers copied onto each row, and the
    /// screen needs them once — including for a predavač with no oglasi at all, who has no row
    /// to carry them. It asks <see cref="IOcenaRepository"/> for them instead.
    /// </remarks>
    Task<IReadOnlyList<Oglas>> AktivniZaPredavacaAsync(string predavacId);

    /// <summary>
    /// Everything one predavač has ever published, withdrawn oglasi included — their own
    /// "Moji oglasi" screen, which is the one place a deactivated oglas must still appear so
    /// its owner can bring it back.
    /// </summary>
    Task<IReadOnlyList<Oglas>> SviZaPredavacaAsync(string predavacId);

    /// <summary>Every oglas on the platform, deactivated ones included — the admin listing.</summary>
    Task<IReadOnlyList<Oglas>> SviUkljucujuciNeaktivneAsync();

    /// <summary>
    /// Withdraws an oglas from search. Nothing is deleted (ADR-0001) — this flips
    /// <c>Aktivan</c> and saves. <c>false</c> if there is no oglas with that id.
    /// </summary>
    Task<bool> DeaktivirajAsync(int id);

    /// <summary>Puts a withdrawn oglas back into search. <c>false</c> if there is no oglas with that id.</summary>
    Task<bool> AktivirajAsync(int id);

    /// <summary>
    /// How many oglasi are visible right now — the „broj aktivnih oglasa“ on the statistika
    /// screen.
    /// </summary>
    /// <remarks>
    /// Visible in the same sense the pretraga means it, the predavač's flag included, so the
    /// number an administrator reads is the number of oglasi an učenik can actually find. An
    /// oglas that is <c>Aktivan</c> under a blocked predavač is in neither, which is the one
    /// case where „aktivan“ on the row and „aktivan“ on the platform part ways.
    /// </remarks>
    Task<int> BrojVidljivihAsync();

    /// <summary>
    /// The predmeti carrying the most visible oglasi, most first — at most
    /// <paramref name="koliko"/> rows.
    /// </summary>
    /// <remarks>
    /// A ranking of supply, not of demand: ocene attach to the predavač and never to the
    /// oglas, so nothing here can say which predmet učenici want most. See
    /// <see cref="StavkaStatistike"/>. Predmeti tied on count are ordered by naziv, so the
    /// table does not reshuffle between two reads of the same data.
    /// </remarks>
    Task<IReadOnlyList<StavkaStatistike>> TopPredmetiAsync(int koliko);

    /// <summary>The same ranking one level up: the oblasti carrying the most visible oglasi.</summary>
    Task<IReadOnlyList<StavkaStatistike>> TopOblastiAsync(int koliko);
}
