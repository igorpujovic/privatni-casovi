using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// The predavači an učenik has marked to find again. Private to that učenik: there is no
/// method here that answers "who favourited this predavač", and there is not meant to be.
/// </summary>
/// <remarks>
/// This one does not extend <see cref="IRepository{T}"/>. An omiljeni row is a pair and
/// nothing else — it has no surrogate id to fetch it by, and it is only ever added or
/// removed whole, so the generic by-key primitives have nothing to offer it.
/// </remarks>
public interface IOmiljeniRepository
{
    /// <summary>
    /// The predavači on one učenik's list, by prezime. A predavač who has since been blocked
    /// drops off the list without the row being touched.
    /// </summary>
    Task<IReadOnlyList<Korisnik>> ZaUcenikaAsync(string ucenikId);

    /// <summary>Whether this predavač is already on this učenik's list — the state of the toggle.</summary>
    /// <remarks>
    /// The one read here that does <b>not</b> filter on <c>Aktivan</c>, unlike
    /// <see cref="ZaUcenikaAsync"/> just above it, and deliberately so. ADR-0001 puts that
    /// filter on every query that reaches korisnici; this one reaches a row and answers
    /// whether it is there. Whether the predavač is visible is
    /// <c>IKorisnikRepository.PredavacAsync</c>'s answer, and every caller of this has already
    /// had it — a blocked predavač 404s before the toggle is drawn or pressed.
    /// <para>
    /// Filtering here would also break <see cref="DodajAsync"/>, whose duplicate guard this
    /// is: it would report "not on the list" for a pair that is on it, and send an insert at
    /// a primary key that would refuse it.
    /// </para>
    /// </remarks>
    Task<bool> PostojiAsync(string ucenikId, string predavacId);

    /// <summary>Adds the pair if it is not already there. Doing it twice is not an error.</summary>
    Task DodajAsync(string ucenikId, string predavacId);

    /// <summary>Removes the pair if it is there. Doing it twice is not an error.</summary>
    Task UkloniAsync(string ucenikId, string predavacId);
}
