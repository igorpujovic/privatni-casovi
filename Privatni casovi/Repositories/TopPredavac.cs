using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// One row of the top-predavači ranking: a predavač, how many ocene they have been given, and
/// what those ocene average to.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProsecnaOcena"/> is a plain <c>decimal</c> here and nullable everywhere else on
/// the platform, which is not an inconsistency: the ranking is built by grouping ocene, so a
/// predavač nobody has rated has no group and is therefore not a row at all. „Nema ocena“ is a
/// state this record cannot represent because it cannot reach it — the two seeded unrated
/// predavači are simply absent from the table rather than sorted to the bottom of it.
/// </para>
/// <para>
/// The whole <see cref="Korisnik"/> rides along rather than a name, because an administrator
/// reading a ranking wants to open what is on it and the row needs an id to link with.
/// </para>
/// </remarks>
/// <param name="BrojOcena">
/// What the ranking is ordered by. Never the average alone — see
/// <see cref="IOcenaRepository.TopPredavaciAsync"/>.
/// </param>
public record TopPredavac(Korisnik Predavac, int BrojOcena, decimal ProsecnaOcena);
