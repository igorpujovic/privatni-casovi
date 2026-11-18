using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

public class PredmetRepository : Repository<Predmet>, IPredmetRepository
{
    public PredmetRepository(ApplicationDbContext db) : base(db)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Predmeti carry no Aktivan flag — the list is closed and seeded — so this only adds the
    /// oblast every caller needs alongside, and an order.
    /// </remarks>
    public override async Task<IReadOnlyList<Predmet>> GetAllAsync()
        => await _db.Predmeti
            .Include(p => p.Oblast)
            .OrderBy(p => p.Oblast!.Naziv)
            .ThenBy(p => p.Naziv)
            .ToListAsync();

    public async Task<IReadOnlyList<Oblast>> OblastiAsync()
        => await _db.Oblasti.AsNoTracking().OrderBy(o => o.Naziv).ToListAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <c>AsNoTracking</c> is what makes the <c>OrderBy</c> inside the <c>Include</c> hold, and
    /// it is not an optimisation. A tracked query hands back the <see cref="Oblast"/> instances
    /// the context already has, predmeti and all — so on a screen that ran a query over oglasi
    /// first, the dropdown came out in the order those oglasi happened to mention their
    /// predmeti, not by naziv. Untracked, the ordering the query asked for is the ordering the
    /// caller gets, whatever else was read on the same request.
    /// </para>
    /// <para>
    /// Nothing writes an oblast or a predmet — both lists are seeded and closed — so there is
    /// nothing here that tracking would have been for.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<Oblast>> OblastiSaPredmetimaAsync()
        => await _db.Oblasti
            .AsNoTracking()
            .Include(o => o.Predmeti.OrderBy(p => p.Naziv))
            .OrderBy(o => o.Naziv)
            .ToListAsync();
}
