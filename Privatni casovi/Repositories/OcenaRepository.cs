using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

public class OcenaRepository : Repository<Ocena>, IOcenaRepository
{
    public OcenaRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<decimal?> ProsecnaOcenaAsync(string predavacId)
        => await _db.Ocene
            .Where(o => o.PredavacId == predavacId)
            // The cast is what makes an unrated predavač come back as null: AVG over no rows
            // is NULL in SQL, and Average(x => x.Vrednost) on an int would throw instead.
            .AverageAsync(o => (decimal?)o.Vrednost);

    public Task<int> BrojOcenaAsync(string predavacId)
        => _db.Ocene.CountAsync(o => o.PredavacId == predavacId);

    public async Task<IReadOnlyList<int>> ZaPredavacaAsync(string predavacId)
        => await _db.Ocene
            .Where(o => o.PredavacId == predavacId)
            .OrderByDescending(o => o.Id)
            .Select(o => o.Vrednost)
            .ToListAsync();

    public Task<Ocena?> PostojecaAsync(string ucenikId, string predavacId)
        => _db.Ocene.SingleOrDefaultAsync(o => o.UcenikId == ucenikId && o.PredavacId == predavacId);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The grouped counts are joined back to the korisnici rather than grouped by the predavač
    /// entity itself, which EF will not translate: a <c>GROUP BY</c> can key on columns, and
    /// re-materialising a whole <see cref="Korisnik"/> out of that key is not something it can
    /// undo. Joining afterwards asks the database for the same thing in the shape it answers
    /// in, and keeps this to one round trip.
    /// </para>
    /// <para>
    /// The ordering is applied after the join and before <c>Take</c>, which is the only place
    /// it is correct — ordering inside the grouped subquery and then joining would let the
    /// join hand the rows back in whatever order it pleased, and there would be nothing on
    /// screen to show it had.
    /// </para>
    /// <para>
    /// Prezime is a third key under the two the spec names, for the reason
    /// <see cref="IOglasRepository.TopPredmetiAsync"/> orders its ties by naziv: two predavači
    /// on the same count <i>and</i> the same average are otherwise handed back in whatever
    /// order the database picked that time, and the table would reshuffle between two reads of
    /// data that never changed.
    /// </para>
    /// <para>
    /// <c>(decimal)</c> and not <c>(decimal?)</c>, unlike every other average in this
    /// repository: these groups are built out of ocene, so each one has at least one row and
    /// there is no empty average to come back null.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<TopPredavac>> TopPredavaciAsync(int koliko)
    {
        // A blocked predavač is left out of the ranking; a blocked učenik's ocena still counts
        // in it. See the interface — that asymmetry is ADR-0001 applied to the korisnik the
        // row is about, not to the one who left the score.
        var grupe = from ocena in _db.Ocene
                    where ocena.Predavac!.Aktivan
                    group ocena by ocena.PredavacId into g
                    select new
                    {
                        PredavacId = g.Key,
                        BrojOcena = g.Count(),
                        ProsecnaOcena = g.Average(o => (decimal)o.Vrednost)
                    };

        var rangirani = from grupa in grupe
                        join predavac in _db.Users on grupa.PredavacId equals predavac.Id
                        orderby grupa.BrojOcena descending,
                                grupa.ProsecnaOcena descending,
                                predavac.Prezime,
                                predavac.Ime
                        select new TopPredavac(predavac, grupa.BrojOcena, grupa.ProsecnaOcena);

        return await rangirani.Take(koliko).ToListAsync();
    }
}
