using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

/// <summary>
/// Reads over the two seeded, closed lists — predmeti and the oblasti they belong to.
/// Nothing in the application creates either, so this repository only reads.
/// </summary>
/// <remarks>
/// Oblasti live here rather than in a repository of their own: an oblast is never shown or
/// chosen except as the grouping above a predmet, so splitting them would mean injecting two
/// repositories everywhere one dropdown is rendered.
/// </remarks>
public interface IPredmetRepository : IRepository<Predmet>
{
    /// <summary>Every oblast, by naziv — the oblast filter on the pretraga form.</summary>
    Task<IReadOnlyList<Oblast>> OblastiAsync();

    /// <summary>
    /// Every oblast with its predmeti loaded, both by naziv. The shape the predmet dropdown
    /// wants: one <c>&lt;select&gt;</c> with an <c>&lt;optgroup&gt;</c> per oblast, which a
    /// view can render by nesting two loops with no grouping logic and no JavaScript.
    /// </summary>
    Task<IReadOnlyList<Oblast>> OblastiSaPredmetimaAsync();
}
