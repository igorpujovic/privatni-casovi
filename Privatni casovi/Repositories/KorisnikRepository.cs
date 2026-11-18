using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Models;

namespace Privatni_casovi.Repositories;

public class KorisnikRepository : Repository<Korisnik>, IKorisnikRepository
{
    public KorisnikRepository(ApplicationDbContext db) : base(db)
    {
    }

    /// <inheritdoc/>
    /// <remarks>Overridden so that "every korisnik" means every korisnik who is not blocked.</remarks>
    public override async Task<IReadOnlyList<Korisnik>> GetAllAsync()
        => await _db.Users.Where(k => k.Aktivan).OrderBy(k => k.Prezime).ThenBy(k => k.Ime).ToListAsync();

    public async Task<IReadOnlyList<Korisnik>> PredavaciAsync()
        => await UUlozi(Uloge.Predavac)
            .Where(k => k.Aktivan)
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .ToListAsync();

    /// <inheritdoc/>
    /// <remarks>
    /// The blank id is short-circuited rather than queried. The database would answer nothing
    /// for it anyway, but answering it here is what lets every caller be the same two lines:
    /// all four ways of being nobody — no id, an učenik or an administrator, a blocked
    /// predavač, an id belonging to no one — come back as one <c>null</c>.
    /// </remarks>
    public Task<Korisnik?> PredavacAsync(string? id)
        => string.IsNullOrWhiteSpace(id)
            ? Task.FromResult<Korisnik?>(null)
            : UUlozi(Uloge.Predavac).SingleOrDefaultAsync(k => k.Id == id && k.Aktivan);

    public async Task<IReadOnlyList<Korisnik>> SviUkljucujuciNeaktivneAsync()
        => await _db.Users.OrderBy(k => k.Prezime).ThenBy(k => k.Ime).ToListAsync();

    public async Task<IReadOnlyDictionary<string, string>> UlogePoKorisnikuAsync()
    {
        var parovi = await (from ur in _db.UserRoles
                            join r in _db.Roles on ur.RoleId equals r.Id
                            select new { ur.UserId, r.Name }).ToListAsync();

        // A korisnik holds exactly one role, fixed at registration, so the last-wins here is
        // never actually exercised — it is only there to keep ToDictionary from throwing if
        // the database is ever hand-edited into a state the application cannot produce.
        var uloge = new Dictionary<string, string>();

        foreach (var par in parovi)
        {
            uloge[par.UserId] = par.Name ?? string.Empty;
        }

        return uloge;
    }

    public async Task<bool> PostaviAktivnostAsync(string id, bool aktivan)
    {
        var korisnik = await _db.Users.SingleOrDefaultAsync(k => k.Id == id);

        if (korisnik is null)
        {
            return false;
        }

        korisnik.Aktivan = aktivan;
        await _db.SaveChangesAsync();
        return true;
    }

    private IQueryable<Korisnik> UUlozi(string uloga)
        => from k in _db.Users
           join ur in _db.UserRoles on k.Id equals ur.UserId
           join r in _db.Roles on ur.RoleId equals r.Id
           where r.Name == uloga
           select k;

    /// <inheritdoc/>
    /// <remarks>
    /// The <c>Aktivan</c> filter is on the korisnik and not on the role: a blocked korisnik
    /// keeps their uloga — nothing is deleted (ADR-0001) — and it is the korisnik that has
    /// stopped counting.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, int>> BrojAktivnihPoUloziAsync()
    {
        var grupe = await (from k in _db.Users
                           where k.Aktivan
                           join ur in _db.UserRoles on k.Id equals ur.UserId
                           join r in _db.Roles on ur.RoleId equals r.Id
                           group k by r.Name! into g
                           select new { Uloga = g.Key, Broj = g.Count() }).ToListAsync();

        return grupe.ToDictionary(g => g.Uloga, g => g.Broj);
    }
}
