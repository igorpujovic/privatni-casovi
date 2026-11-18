namespace Privatni_casovi.Repositories;

/// <summary>
/// The data-access primitives every entity shares. Business logic lives above this and LINQ
/// lives below it — no controller in this application composes a query of its own.
/// </summary>
/// <remarks>
/// <see cref="GetAllAsync"/> is the <i>visible</i> set: where an entity carries an
/// <c>Aktivan</c> flag, the implementation filters on it, so a caller cannot forget to (see
/// ADR-0001). Reads that must see deactivated rows — the admin screens, and a predavač's own
/// list of oglasi they withdrew — say so through a separate, named method on the specific
/// repository.
/// </remarks>
public interface IRepository<T> where T : class
{
    /// <summary>Every visible row. Deactivated rows are not in here.</summary>
    Task<IReadOnlyList<T>> GetAllAsync();

    /// <summary>
    /// The row with this primary key, deactivated or not, with no navigations loaded.
    /// </summary>
    /// <remarks>
    /// This is the raw key lookup, deliberately unfiltered: an administrator reactivating an
    /// oglas and a predavač editing one they withdrew both need a row
    /// <see cref="GetAllAsync"/> would hide. Anything rendered to a korisnik who is not its
    /// owner should come from a filtered method instead — for an oglas that is
    /// <see cref="IOglasRepository.AktivanAsync"/>.
    /// </remarks>
    Task<T?> GetByIdAsync(object id);

    /// <summary>Stages a new row. It reaches the database on <see cref="SaveChangesAsync"/>.</summary>
    void Add(T entity);

    /// <summary>Stages an edit. It reaches the database on <see cref="SaveChangesAsync"/>.</summary>
    void Update(T entity);

    /// <summary>
    /// Writes everything staged on this request. <see cref="Add"/> and <see cref="Update"/>
    /// deliberately do not save on their own: a controller that edits an oglas and then finds
    /// the signed-in korisnik does not own it must be able to walk away having written
    /// nothing.
    /// </summary>
    Task SaveChangesAsync();
}
