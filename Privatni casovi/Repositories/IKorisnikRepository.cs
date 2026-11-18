using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// Reads and writes over korisnici. Blocked korisnici are invisible to everything except the
/// two methods that name them, which is what makes blocking take effect everywhere at once.
/// </summary>
/// <remarks>
/// Identity's <c>UserManager</c> owns creating korisnici, passwords and role membership; this
/// repository owns querying them. The role a korisnik holds is read here rather than through
/// <c>UserManager</c> only where it is a filter on a listing — one join beats one round trip
/// per korisnik.
/// </remarks>
public interface IKorisnikRepository : IRepository<Korisnik>
{
    /// <summary>Every predavač who is not blocked, by prezime.</summary>
    Task<IReadOnlyList<Korisnik>> PredavaciAsync();

    /// <summary>
    /// One predavač for their public profile, or <c>null</c> if that id is not a predavač, is
    /// blocked, or is nothing at all. An učenik must not be able to reach a blocked predavač
    /// by typing their id.
    /// </summary>
    /// <remarks>
    /// The id is nullable and a blank one answers <c>null</c> like any other stranger, which
    /// is the point: this is the whole of the resolve-or-404 shared by the three actions that
    /// take a predavač id from outside — <c>PredavacController.Profil</c>,
    /// <c>OcenaController.Oceni</c> and <c>OmiljeniController</c>'s two writes. Each of them
    /// is then the same two lines — ask here, <c>NotFound()</c> on <c>null</c> — and telling
    /// "not a predavač" apart from "blocked" from "nobody" is exactly what one answer for all
    /// of them prevents.
    /// </remarks>
    Task<Korisnik?> PredavacAsync(string? id);

    /// <summary>Every korisnik, blocked ones included — the admin listing.</summary>
    Task<IReadOnlyList<Korisnik>> SviUkljucujuciNeaktivneAsync();

    /// <summary>The role each korisnik holds, keyed by korisnik id. Every korisnik holds exactly one.</summary>
    Task<IReadOnlyDictionary<string, string>> UlogePoKorisnikuAsync();

    /// <summary>
    /// Blocks or unblocks a korisnik. Nothing is deleted (ADR-0001) — this flips
    /// <c>Aktivan</c> and saves. <c>false</c> if there is no korisnik with that id.
    /// </summary>
    Task<bool> PostaviAktivnostAsync(string id, bool aktivan);

    /// <summary>
    /// How many korisnici who are not blocked hold each uloga, keyed by the role string — the
    /// „broj korisnika“ on the statistika screen, split the way the brief asks for it.
    /// </summary>
    /// <remarks>
    /// One grouped read rather than three counted ones, and rather than counting a column on
    /// <see cref="Korisnik"/> that does not exist: the uloga is Identity role membership, two
    /// tables away, the same join <see cref="UlogePoKorisnikuAsync"/> makes.
    /// <para>
    /// An uloga nobody active holds is <b>missing from the dictionary rather than present as
    /// zero</b> — a <c>GROUP BY</c> produces no row for an empty group. Read it with
    /// <c>GetValueOrDefault</c>; blocking the only administrator is enough to reach that.
    /// </para>
    /// </remarks>
    Task<IReadOnlyDictionary<string, int>> BrojAktivnihPoUloziAsync();
}
