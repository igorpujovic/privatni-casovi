using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

public class OglasRepository : Repository<Oglas>, IOglasRepository
{
    public OglasRepository(ApplicationDbContext db) : base(db)
    {
    }

    /// <inheritdoc/>
    /// <remarks>Overridden so that "every oglas" means every <i>visible</i> oglas.</remarks>
    public override async Task<IReadOnlyList<Oglas>> GetAllAsync()
        => await Vidljivi().OrderByDescending(o => o.DatumKreiranja).ToListAsync();

    public async Task<IReadOnlyList<OglasSaOcenom>> PretragaAsync(
        int? oblastId, int? predmetId, Lokacija? lokacija, decimal? maxCena, string? sortBy)
    {
        var upit = Vidljivi();

        // A null parameter is not a filter — the unfiltered pretraga is the home page.
        if (oblastId is not null)
        {
            upit = upit.Where(o => o.Predmet!.OblastId == oblastId);
        }

        if (predmetId is not null)
        {
            upit = upit.Where(o => o.PredmetId == predmetId);
        }

        if (lokacija is not null)
        {
            // An učenik looking for online časovi is also served by a predavač who teaches
            // both ways, so Oboje answers either request.
            upit = upit.Where(o => o.Lokacija == lokacija || o.Lokacija == Lokacija.Oboje);
        }

        if (maxCena is not null)
        {
            upit = upit.Where(o => o.CenaCasa <= maxCena);
        }

        return await SaOcenom(Sortirano(upit, sortBy)).ToListAsync();
    }

    public Task<OglasSaOcenom?> AktivanAsync(int id)
        => SaOcenom(Vidljivi().Where(o => o.Id == id)).SingleOrDefaultAsync();

    public async Task<IReadOnlyList<Oglas>> AktivniZaPredavacaAsync(string predavacId)
        => await Sortirano(Vidljivi().Where(o => o.PredavacId == predavacId), Sortiranje.Najnoviji)
            .Include(o => o.Predmet)
            .ThenInclude(p => p!.Oblast)
            .ToListAsync();

    public async Task<IReadOnlyList<Oglas>> SviZaPredavacaAsync(string predavacId)
        => await _db.Oglasi
            .Where(o => o.PredavacId == predavacId)
            .Include(o => o.Predmet)
            .ThenInclude(p => p!.Oblast)
            .OrderByDescending(o => o.DatumKreiranja)
            .ToListAsync();

    public async Task<IReadOnlyList<Oglas>> SviUkljucujuciNeaktivneAsync()
        => await _db.Oglasi
            .Include(o => o.Predmet)
            .ThenInclude(p => p!.Oblast)
            .Include(o => o.Predavac)
            .OrderByDescending(o => o.DatumKreiranja)
            .ToListAsync();

    public Task<bool> DeaktivirajAsync(int id) => PostaviAktivnostAsync(id, aktivan: false);

    public Task<bool> AktivirajAsync(int id) => PostaviAktivnostAsync(id, aktivan: true);

    public Task<int> BrojVidljivihAsync() => Vidljivi().CountAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The key is the predmet's id together with its naziv, not the naziv alone. Two predmeti
    /// in two different oblasti are two predmeti even if they are spelled the same, and
    /// grouping on the displayed string would silently add them together. The naziv rides
    /// along in the key because a column the grouping did not group by cannot be selected out
    /// of the group afterwards.
    /// </para>
    /// <para>
    /// Ties break on naziv so the table is stable between two reads of the same data — which
    /// matters more here than it sounds: the seeded platform has five predmeti on two oglasi
    /// each, so almost the whole of this table is a tie. The ordering is written before the
    /// projection for the reason <see cref="Sortirano"/> gives at length — an <c>ORDER BY</c>
    /// over a projection that constructs a type does not translate.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<StavkaStatistike>> TopPredmetiAsync(int koliko)
        => await Vidljivi()
            .GroupBy(o => new { o.PredmetId, o.Predmet!.Naziv })
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.Naziv)
            .Select(g => new StavkaStatistike(g.Key.Naziv, g.Count()))
            .Take(koliko)
            .ToListAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// The same query one join further out, written out rather than shared with
    /// <see cref="TopPredmetiAsync"/>. The two differ only in their grouping key, and a key is
    /// the one part of a LINQ query that cannot be passed in as a parameter here: an anonymous
    /// type is what <c>GROUP BY</c> translates from, and an anonymous type cannot satisfy a
    /// generic constraint that would let a shared method read <c>Naziv</c> off it.
    /// </remarks>
    public async Task<IReadOnlyList<StavkaStatistike>> TopOblastiAsync(int koliko)
        => await Vidljivi()
            .GroupBy(o => new { o.Predmet!.OblastId, o.Predmet.Oblast!.Naziv })
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.Naziv)
            .Select(g => new StavkaStatistike(g.Key.Naziv, g.Count()))
            .Take(koliko)
            .ToListAsync();

    /// <summary>
    /// The oglasi an učenik is allowed to see: published, by a korisnik who is not blocked.
    /// </summary>
    /// <remarks>
    /// The predavač's own <c>Aktivan</c> matters as much as the oglas's — blocking a predavač
    /// has to take their oglasi out of search, and their oglasi are never deactivated one by
    /// one when that happens.
    /// </remarks>
    private IQueryable<Oglas> Vidljivi()
        => _db.Oglasi.Where(o => o.Aktivan && o.Predavac!.Aktivan);

    /// <summary>
    /// Applies one of the <see cref="Sortiranje"/> orders. Anything unrecognised, including
    /// <c>null</c>, sorts newest-first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This sorts the oglasi and not the <see cref="OglasSaOcenom"/>s that
    /// <see cref="SaOcenom"/> produces, which is not a style choice: EF cannot translate an
    /// <c>ORDER BY</c> written over a projection that constructs a type — it inlines the whole
    /// constructor into the clause and then gives up. Ordering first and projecting second
    /// reads the same and generates the <c>ORDER BY</c> that was wanted, because a projection
    /// keeps the order of the query it is applied to.
    /// </para>
    /// <para>
    /// The ocene subqueries below are spelled out again in <see cref="SaOcenom"/> rather than
    /// shared, and are a third statement of what <see cref="IOcenaRepository"/> already
    /// computes. That is forced: an expression tree has to be handed to the provider whole, so
    /// it cannot call into another repository — and the alternative, asking for each
    /// predavač's ocene one card at a time, is a query per result.
    /// </para>
    /// </remarks>
    private IQueryable<Oglas> Sortirano(IQueryable<Oglas> upit, string? sortBy)
        => sortBy switch
        {
            Sortiranje.CenaRastuce => upit.OrderBy(o => o.CenaCasa).ThenByDescending(o => o.DatumKreiranja),
            Sortiranje.CenaOpadajuce => upit.OrderByDescending(o => o.CenaCasa).ThenByDescending(o => o.DatumKreiranja),

            // An unrated predavač has no prosečna ocena at all, so this subquery is NULL
            // rather than zero, and SQL Server sorts NULL last under DESC. That is the rule
            // the spec asks for — sorted after every rated predavač — and it holds only as
            // long as the average stays nullable all the way down into the database.
            Sortiranje.OcenaOpadajuce => upit
                .OrderByDescending(o => _db.Ocene
                    .Where(x => x.PredavacId == o.PredavacId)
                    .Average(x => (decimal?)x.Vrednost))
                .ThenByDescending(o => o.DatumKreiranja),

            Sortiranje.BrojOcenaOpadajuce => upit
                .OrderByDescending(o => _db.Ocene.Count(x => x.PredavacId == o.PredavacId))
                .ThenByDescending(o => o.DatumKreiranja),

            _ => upit.OrderByDescending(o => o.DatumKreiranja)
        };

    /// <summary>
    /// Attaches each oglas's predavač standing, computed in the same round trip rather than
    /// once per card, and loads the predmet, oblast and predavač a result is rendered with.
    /// </summary>
    private IQueryable<OglasSaOcenom> SaOcenom(IQueryable<Oglas> upit)
        => upit
            .Include(o => o.Predmet)
            .ThenInclude(p => p!.Oblast)
            .Include(o => o.Predavac)
            .Select(o => new OglasSaOcenom(
                o,
                // AVG over no rows is NULL, which is exactly "nema ocena".
                _db.Ocene.Where(x => x.PredavacId == o.PredavacId).Average(x => (decimal?)x.Vrednost),
                _db.Ocene.Count(x => x.PredavacId == o.PredavacId)));

    private async Task<bool> PostaviAktivnostAsync(int id, bool aktivan)
    {
        var oglas = await _db.Oglasi.FindAsync(id);

        if (oglas is null)
        {
            return false;
        }

        oglas.Aktivan = aktivan;
        await _db.SaveChangesAsync();
        return true;
    }
}
