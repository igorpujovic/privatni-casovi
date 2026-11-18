using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// An oglas together with the standing of the predavač who published it — what a search
/// result card, an oglas detail page and a profile listing all need to show.
/// </summary>
/// <remarks>
/// The ocene ride along because they are computed, never stored (see <c>CONTEXT.md</c>,
/// "Prosečna ocena"): they are read in the same query as the oglasi rather than one extra
/// round trip per card. <see cref="Oglas"/> arrives with its <c>Predmet</c>, that predmet's
/// <c>Oblast</c> and its <c>Predavac</c> already loaded.
/// </remarks>
/// <param name="ProsecnaOcena">
/// <c>null</c> when nobody has rated the predavač — displayed "Nema ocena" and never 0.0.
/// </param>
public record OglasSaOcenom(Oglas Oglas, decimal? ProsecnaOcena, int BrojOcena);
