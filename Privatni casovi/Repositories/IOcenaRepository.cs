using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// Reads and writes over ocene. Nothing here ever loads the <c>Ucenik</c> behind an ocena: a
/// predavač sees the scores they were given and never who gave them, and the cheapest way to
/// keep that true is for the identity never to leave this layer.
/// </summary>
public interface IOcenaRepository : IRepository<Ocena>
{
    /// <summary>
    /// The mean of every ocena a predavač has received, or <c>null</c> if nobody has rated
    /// them — never 0.0, which would read as a bad predavač rather than a new one.
    /// </summary>
    /// <remarks>Computed on every read, never stored, so it moves the moment a rating changes.</remarks>
    Task<decimal?> ProsecnaOcenaAsync(string predavacId);

    /// <summary>How many ocene a predavač has. Shown next to the average, which alone says little.</summary>
    Task<int> BrojOcenaAsync(string predavacId);

    /// <summary>
    /// The scores one predavač received, for their own "Moje ocene" screen.
    /// </summary>
    /// <remarks>
    /// Anonymous by construction: this returns the vrednosti and nothing else, so there is no
    /// <c>UcenikId</c> on the way out for a view to render by accident. Leaving the
    /// <c>Ucenik</c> navigation unloaded would not have been enough — the id is a column on
    /// the ocena itself.
    /// </remarks>
    Task<IReadOnlyList<int>> ZaPredavacaAsync(string predavacId);

    /// <summary>
    /// The ocena this učenik already gave this predavač, or <c>null</c>. An učenik has at
    /// most one per predavač; rating again edits this row rather than adding a second.
    /// </summary>
    Task<Ocena?> PostojecaAsync(string ucenikId, string predavacId);

    /// <summary>
    /// The best-rated predavači for the statistika screen, at most <paramref name="koliko"/> of
    /// them — <b>ordered by how many ocene they have</b>, ties broken by the average.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Never by the average alone, and the order of those two keys is the whole point of the
    /// method: one 5/5 would otherwise outrank a predavač with forty ocene averaging 4,8. The
    /// count is the confidence and the average is the tie-break, so a predavač reaches the top
    /// of this table by having been rated a lot and rated well, in that order.
    /// </para>
    /// <para>
    /// Blocked <i>predavači</i> are left out — ADR-0001 has statistics count active records
    /// only, and a ranking that names a korisnik whose profil 404s sends an administrator
    /// somewhere they cannot go. Blocked <i>učenici</i> are not: their ocene go on counting
    /// here exactly as they go on counting in
    /// <see cref="ProsecnaOcenaAsync"/>, which is this repository filtering on nothing about
    /// the person who left a score. Dropping them would move the numbers on this screen away
    /// from the numbers on the profil predavača it ranks.
    /// </para>
    /// <para>
    /// A predavač nobody has rated is absent rather than last: this groups ocene, and they
    /// have none to group. That is the one place the platform's „Nema ocena“ rule does not
    /// apply, because a ranking by broj ocena has nothing to say about a predavač with zero.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<TopPredavac>> TopPredavaciAsync(int koliko);
}
