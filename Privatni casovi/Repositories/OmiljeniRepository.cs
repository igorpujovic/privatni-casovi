using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

public class OmiljeniRepository : IOmiljeniRepository
{
    private readonly ApplicationDbContext _db;

    public OmiljeniRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Korisnik>> ZaUcenikaAsync(string ucenikId)
        => await _db.Omiljeni
            .Where(o => o.UcenikId == ucenikId && o.Predavac!.Aktivan)
            .Select(o => o.Predavac!)
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .ToListAsync();

    public Task<bool> PostojiAsync(string ucenikId, string predavacId)
        => _db.Omiljeni.AnyAsync(o => o.UcenikId == ucenikId && o.PredavacId == predavacId);

    /// <remarks>
    /// Checked and caught, not one or the other. The check is what makes the ordinary second
    /// press cost one query and no exception; the catch is for the press the check cannot
    /// cover — two requests racing, where both look, both find nothing, and both insert. That
    /// race is real and was reproduced: sixteen concurrent presses of this button put two of
    /// them past the check together, and the composite primary key answered the loser with
    /// <c>Violation of PRIMARY KEY constraint 'PK_Omiljeni'</c> — a 500 for a button that had
    /// in fact done exactly what it was pressed to do.
    /// </remarks>
    public async Task DodajAsync(string ucenikId, string predavacId)
    {
        if (await PostojiAsync(ucenikId, predavacId))
        {
            return;
        }

        _db.Omiljeni.Add(new Omiljeni { UcenikId = ucenikId, PredavacId = predavacId });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            // Forgiven only when the pair is actually there. The race is the failure this
            // catch exists for, and the pair being on the list is what identifies it — the
            // other request wrote the row, which is the whole of what this method promises,
            // so there is nothing to tell the učenik about a button that worked.
            //
            // Anything else is rethrown, and the reason is a bug this catch had when it was
            // written: swallowing every DbUpdateException meant a refused write still came
            // back "Predavač je dodat u omiljene." with no row behind it. Reproduced against
            // a CHECK constraint the insert could not satisfy — 302, the success alert, and
            // zero rows. A 500 on a database that is genuinely broken is what every other
            // write on this platform does, and it is not a lie.
            if (!await PostojiAsync(ucenikId, predavacId))
            {
                throw;
            }
        }
    }

    /// <remarks>
    /// <para>
    /// A real delete, and the only one in the application — see the Scope paragraph of
    /// ADR-0001, which puts omiljeni outside the soft-delete rule. An omiljeni row is one
    /// učenik's private bookmark, so unmarking a predavač has to actually unmark them.
    /// </para>
    /// <para>
    /// The mirror of <see cref="DodajAsync"/>, and racing the same way: two requests can both
    /// load the row and both delete it, and the loser's <c>DELETE</c> matches nothing, which
    /// EF reads as a lost update and raises rather than shrugs at. The pair is gone either
    /// way, which is what was asked for.
    /// </para>
    /// </remarks>
    public async Task UkloniAsync(string ucenikId, string predavacId)
    {
        var omiljeni = await _db.Omiljeni
            .SingleOrDefaultAsync(o => o.UcenikId == ucenikId && o.PredavacId == predavacId);

        if (omiljeni is null)
        {
            return;
        }

        _db.Omiljeni.Remove(omiljeni);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Needs no re-check, unlike DodajAsync's catch. EF raises this exact type only
            // when the DELETE matched no row, and a row that is not there is precisely what
            // this method was asked to bring about. Every other way a save can fail is a
            // DbUpdateException that is not this subtype, and none of them are caught here.
            _db.ChangeTracker.Clear();
        }
    }
}
