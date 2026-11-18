using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Models;

namespace Privatni_casovi.Data;

/// <summary>
/// Fills an empty database with the data the application is browsed and graded on — oblasti,
/// predmeti, korisnici of all three uloge, oglasi, ocene and omiljeni.
/// </summary>
/// <remarks>
/// <para>
/// Korisnici are created through <see cref="UserManager{TUser}"/> rather than written as rows,
/// which is the whole reason this is a runtime seeder and not <c>HasData</c> in a migration:
/// only <c>UserManager</c> produces a password hash Identity will actually accept, and the
/// credentials handed in with the project have to work.
/// </para>
/// <para>
/// The data is deliberately lopsided, because a uniform dataset demonstrates nothing. It
/// contains two predavači nobody has rated (so "Nema ocena" and the sort-last rule are
/// visible), one deactivated oglas and one blocked predavač (so the <c>Aktivan</c> flag of
/// ADR-0001 is visible on both entities), two predavači with no photo, and oglasi across all
/// three <see cref="Lokacija"/> values.
/// </para>
/// </remarks>
public static class DbInitializer
{
    /// <summary>
    /// The password every seeded korisnik signs in with. One password for all of them, so the
    /// credentials deliverable (issue 16) is three email addresses and this one string.
    /// </summary>
    public const string Lozinka = "lozinka123";

    /// <summary>The seeded administrator. Administrators are never self-registered.</summary>
    public const string EmailAdministratora = "admin@privatnicasovi.rs";

    /// <summary>
    /// The predavač handed in as a credential. Three oglasi, one of them deactivated, and six
    /// ocene — so signing in as them reaches a "Moji oglasi" with something withdrawn on it and
    /// a "Moje ocene" with something in it, which are the two screens a predavač is judged on.
    /// </summary>
    public const string EmailPredavaca = "ana.jovanovic@primer.rs";

    /// <summary>The učenik handed in as a credential — has ocene given and omiljeni marked.</summary>
    public const string EmailUcenika = "milos.petrovic@primer.rs";

    /// <summary>
    /// Seeds the database if it is empty. Safe to call on every startup.
    /// </summary>
    /// <remarks>
    /// The guard covers the whole seed rather than one phase each. The phases run in dependency
    /// order and save as they go, so a run that dies halfway leaves a partly populated database
    /// this guard would then skip — the recovery is to drop the database and start the app
    /// again, which is exactly what this issue's done-when describes anyway. Guarding on
    /// korisnici as well as on oblasti matters: a database that has only been registered into
    /// by hand has no oblasti but does have korisnici, and seeding on top of it would collide
    /// on an email address.
    /// </remarks>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (await db.Oblasti.AnyAsync() || await db.Users.AnyAsync())
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<Korisnik>>();

        var predmeti = await SeedPredmetiAsync(db);
        var predavaci = await SeedKorisniciAsync(userManager, Predavaci, Uloge.Predavac);
        var ucenici = await SeedKorisniciAsync(userManager, Ucenici, Uloge.Ucenik);
        await SeedKorisniciAsync(userManager, new[] { Administrator }, Uloge.Administrator);

        await SeedOglasiAsync(db, predavaci, predmeti);
        await SeedOceneAsync(db, predavaci, ucenici);
        await SeedOmiljeniAsync(db, predavaci, ucenici);
    }

    private static async Task<IReadOnlyDictionary<string, Predmet>> SeedPredmetiAsync(ApplicationDbContext db)
    {
        var predmeti = new Dictionary<string, Predmet>();

        foreach (var (nazivOblasti, nazivi) in Katalog)
        {
            var oblast = new Oblast { Naziv = nazivOblasti };
            db.Oblasti.Add(oblast);

            foreach (var naziv in nazivi)
            {
                var predmet = new Predmet { Naziv = naziv, Oblast = oblast };
                db.Predmeti.Add(predmet);
                predmeti[naziv] = predmet;
            }
        }

        await db.SaveChangesAsync();
        return predmeti;
    }

    /// <summary>
    /// Creates one batch of korisnici in one uloga and returns them keyed by their seed key.
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, Korisnik>> SeedKorisniciAsync(
        UserManager<Korisnik> userManager, IReadOnlyList<SeedKorisnik> seed, string uloga)
    {
        var korisnici = new Dictionary<string, Korisnik>();

        foreach (var opis in seed)
        {
            var korisnik = new Korisnik
            {
                // UserName is the email, the same as registration does it — the brief asks for
                // email + password login, and Identity signs in by UserName.
                UserName = opis.Email,
                Email = opis.Email,
                Ime = opis.Ime,
                Prezime = opis.Prezime,
                Telefon = opis.Telefon,
                Biografija = opis.Biografija,
                SlikaFajl = opis.SlikaFajl,
                Aktivan = opis.Aktivan
            };

            Proveri(await userManager.CreateAsync(korisnik, Lozinka), $"kreiranje korisnika '{opis.Email}'");
            Proveri(await userManager.AddToRoleAsync(korisnik, uloga), $"dodela uloge '{uloga}' korisniku '{opis.Email}'");

            korisnici[opis.Kljuc] = korisnik;
        }

        return korisnici;
    }

    private static async Task SeedOglasiAsync(
        ApplicationDbContext db,
        IReadOnlyDictionary<string, Korisnik> predavaci,
        IReadOnlyDictionary<string, Predmet> predmeti)
    {
        // Dated relative to now rather than to a fixed date, so the newest oglas is always a
        // couple of days old however long after this was written the database is seeded.
        var sada = DateTime.Now;

        foreach (var opis in Oglasi)
        {
            db.Oglasi.Add(new Oglas
            {
                Predavac = predavaci[opis.Predavac],
                Predmet = predmeti[opis.Predmet],
                CenaCasa = opis.CenaCasa,
                Lokacija = opis.Lokacija,
                Dostupnost = opis.Dostupnost,
                DatumKreiranja = sada.AddDays(-opis.PreDana),
                Aktivan = opis.Aktivan
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Hands each rated predavač their scores. The scores are written out per predavač because
    /// their spread is the point — it is what makes <c>ocena_desc</c> visibly reorder the
    /// results. Which učenik gave which score is not the point, so that half is a rotation:
    /// starting at the predavač's own offset and stepping through the učenici keeps every
    /// (učenik, predavač) pair distinct, which the unique index requires.
    /// </summary>
    private static async Task SeedOceneAsync(
        ApplicationDbContext db,
        IReadOnlyDictionary<string, Korisnik> predavaci,
        IReadOnlyDictionary<string, Korisnik> ucenici)
    {
        var redosled = Ucenici.Select(u => ucenici[u.Kljuc]).ToList();

        foreach (var (kljucPredavaca, pomeraj, vrednosti) in Ocene)
        {
            for (var i = 0; i < vrednosti.Length; i++)
            {
                db.Ocene.Add(new Ocena
                {
                    Predavac = predavaci[kljucPredavaca],
                    Ucenik = redosled[(pomeraj + i) % redosled.Count],
                    Vrednost = vrednosti[i]
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedOmiljeniAsync(
        ApplicationDbContext db,
        IReadOnlyDictionary<string, Korisnik> predavaci,
        IReadOnlyDictionary<string, Korisnik> ucenici)
    {
        foreach (var (kljucUcenika, kljucPredavaca) in Omiljeni)
        {
            db.Omiljeni.Add(new Omiljeni
            {
                Ucenik = ucenici[kljucUcenika],
                Predavac = predavaci[kljucPredavaca]
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Fails loudly. Every failure Identity can return here is a bug in this file or a broken
    /// database, and a seed that half-ran in silence is worse than one that stopped startup.
    /// </summary>
    private static void Proveri(IdentityResult rezultat, string radnja)
    {
        if (!rezultat.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed — {radnja} nije uspelo: {string.Join("; ", rezultat.Errors.Select(g => g.Description))}");
        }
    }

    private sealed record SeedKorisnik(
        string Kljuc,
        string Ime,
        string Prezime,
        string Email,
        string Telefon,
        string? Biografija = null,
        string? SlikaFajl = null,
        bool Aktivan = true);

    private sealed record SeedOglas(
        string Predavac,
        string Predmet,
        decimal CenaCasa,
        Lokacija Lokacija,
        string Dostupnost,
        int PreDana,
        bool Aktivan = true);

    /// <summary>The two closed lists: five oblasti and the fifteen predmeti under them.</summary>
    private static readonly (string Oblast, string[] Predmeti)[] Katalog =
    {
        ("Prirodne nauke", new[] { "Matematika", "Fizika", "Hemija", "Biologija" }),
        ("Jezici", new[] { "Engleski jezik", "Nemački jezik", "Srpski jezik i književnost" }),
        ("Informatika", new[] { "Programiranje", "Veb razvoj", "Baze podataka" }),
        ("Društvene nauke", new[] { "Istorija", "Geografija", "Sociologija" }),
        ("Umetnost", new[] { "Klavir", "Gitara" }),
    };

    private static readonly SeedKorisnik Administrator = new(
        "administrator", "Vladimir", "Simonović", EmailAdministratora, "011 555 0100");

    /// <summary>
    /// Ten predavači. <c>dusan</c> and <c>tamara</c> are never rated below and <c>bojan</c> is
    /// blocked — the three that make the empty and deactivated states demonstrable.
    /// <para>
    /// <c>nikola</c> and <c>tamara</c> have no photo. The overlap is deliberate in both
    /// directions: <c>tamara</c> is missing a photo <i>and</i> ocene because she is the
    /// brand-new predavač, which is what a real empty profile looks like, while <c>nikola</c>
    /// is rated and still has no photo — so a view cannot get away with treating "no photo" and
    /// "nema ocena" as the same condition.
    /// </para>
    /// </summary>
    private static readonly SeedKorisnik[] Predavaci =
    {
        new("ana", "Ana", "Jovanović", EmailPredavaca, "064 111 2201",
            "Predajem matematiku i fiziku dvanaest godina, najviše učenicima srednjih škola. Pripremam za maturu i za prijemne ispite na tehničkim fakultetima.",
            "predavac-ana.jpg"),

        new("marko", "Marko", "Nikolić", "marko.nikolic@primer.rs", "064 111 2202",
            "Softverski inženjer, predajem programiranje i baze podataka. Časovi su praktični — pišemo kod od prvog časa, bez suvoparne teorije.",
            "predavac-marko.jpg"),

        new("jelena", "Jelena", "Ilić", "jelena.ilic@primer.rs", "064 111 2203",
            "Diplomirani filolog za engleski i nemački jezik. Radim pripremu za Cambridge i Goethe sertifikate, kao i konverzaciju sa odraslima.",
            "predavac-jelena.jpg"),

        new("stefan", "Stefan", "Đorđević", "stefan.djordjevic@primer.rs", "064 111 2204",
            "Apsolvent fizike. Držim časove matematike i fizike osnovcima i srednjoškolcima, sa naglaskom na razumevanje gradiva umesto učenja napamet.",
            "predavac-stefan.jpg"),

        new("milica", "Milica", "Pavlović", "milica.pavlovic@primer.rs", "064 111 2205",
            "Diplomirala sam srpski jezik i književnost. Pored redovnog gradiva radim pisanje eseja, lektiru i pripremu za malu maturu.",
            "predavac-milica.jpg"),

        new("nikola", "Nikola", "Stojanović", "nikola.stojanovic@primer.rs", "064 111 2206",
            "Veb programer. Predajem izradu sajtova i osnove programiranja, isključivo online, uz deljeni ekran i zajednički rad na projektu."),

        new("ivana", "Ivana", "Marković", "ivana.markovic@primer.rs", "064 111 2207",
            "Predajem biologiju i hemiju. Pripremam učenike za takmičenja i za prijemni ispit na medicinskom fakultetu.",
            "predavac-ivana.jpg"),

        new("dusan", "Dušan", "Ristić", "dusan.ristic@primer.rs", "064 111 2208",
            "Muzičar sa završenom srednjom muzičkom školom. Držim časove klavira i gitare za početnike svih uzrasta, u svom studiju.",
            "predavac-dusan.jpg"),

        new("tamara", "Tamara", "Vasić", "tamara.vasic@primer.rs", "064 111 2209",
            "Predajem engleski i srpski jezik. Nova sam na platformi — radim individualno i u paru, tempom koji dogovorimo."),

        new("bojan", "Bojan", "Simić", "bojan.simic@primer.rs", "064 111 2210",
            "Predajem matematiku i baze podataka.",
            "predavac-bojan.jpg",
            Aktivan: false),
    };

    /// <summary>Ten učenici. Their order here is the rotation <see cref="SeedOceneAsync"/> steps through.</summary>
    private static readonly SeedKorisnik[] Ucenici =
    {
        new("milos", "Miloš", "Petrović", EmailUcenika, "065 333 4401"),
        new("sara", "Sara", "Đukić", "sara.djukic@primer.rs", "065 333 4402"),
        new("luka", "Luka", "Kostić", "luka.kostic@primer.rs", "065 333 4403"),
        new("mina", "Mina", "Radovanović", "mina.radovanovic@primer.rs", "065 333 4404"),
        new("petar", "Petar", "Lukić", "petar.lukic@primer.rs", "065 333 4405"),
        new("teodora", "Teodora", "Mitrović", "teodora.mitrovic@primer.rs", "065 333 4406"),
        new("filip", "Filip", "Janković", "filip.jankovic@primer.rs", "065 333 4407"),
        new("anja", "Anja", "Todorović", "anja.todorovic@primer.rs", "065 333 4408"),
        new("vuk", "Vuk", "Milošević", "vuk.milosevic@primer.rs", "065 333 4409"),
        new("katarina", "Katarina", "Savić", "katarina.savic@primer.rs", "065 333 4410"),
    };

    /// <summary>
    /// Twenty-five oglasi. One is deactivated (ana's Hemija) and two belong to the blocked
    /// bojan, so twenty-two of them are visible in pretraga — that difference is the point.
    /// </summary>
    private static readonly SeedOglas[] Oglasi =
    {
        new("ana", "Matematika", 1800m, Lokacija.Oboje, "Radnim danima od 16 do 20 časova", 2),
        new("ana", "Fizika", 1800m, Lokacija.Online, "Radnim danima posle 18 časova", 5),
        new("ana", "Hemija", 1700m, Lokacija.Uzivo, "Subotom pre podne", 30, Aktivan: false),

        new("marko", "Programiranje", 2500m, Lokacija.Online, "Utorkom i četvrtkom uveče", 1),
        new("marko", "Baze podataka", 2500m, Lokacija.Online, "Vikendom, po dogovoru", 9),
        new("marko", "Veb razvoj", 2800m, Lokacija.Oboje, "Radnim danima od 17 časova", 14),

        new("jelena", "Engleski jezik", 1500m, Lokacija.Oboje, "Svakog dana od 9 do 15 časova", 3),
        new("jelena", "Nemački jezik", 1600m, Lokacija.Online, "Ponedeljkom i sredom uveče", 11),
        new("jelena", "Istorija", 1500m, Lokacija.Uzivo, "Ponedeljkom od 17 časova", 21),

        new("stefan", "Matematika", 1400m, Lokacija.Uzivo, "Radnim danima od 18 do 21 čas", 6),
        new("stefan", "Fizika", 1400m, Lokacija.Uzivo, "Subotom od 10 do 14 časova", 20),

        new("milica", "Srpski jezik i književnost", 1300m, Lokacija.Oboje, "Radnim danima pre podne", 4),
        new("milica", "Istorija", 1300m, Lokacija.Online, "Vikendom od 10 časova", 17),
        new("milica", "Sociologija", 1200m, Lokacija.Online, "Četvrtkom i petkom uveče", 25),

        new("nikola", "Programiranje", 2000m, Lokacija.Online, "Radnim danima posle 20 časova", 8),
        new("nikola", "Veb razvoj", 2200m, Lokacija.Online, "Vikendom, ceo dan", 22),

        new("ivana", "Biologija", 1600m, Lokacija.Uzivo, "Utorkom i četvrtkom od 17 časova", 7),
        new("ivana", "Hemija", 1600m, Lokacija.Oboje, "Radnim danima od 15 do 19 časova", 12),
        new("ivana", "Geografija", 1400m, Lokacija.Online, "Sredom i petkom uveče", 27),

        new("dusan", "Klavir", 2000m, Lokacija.Uzivo, "Ponedeljkom, sredom i petkom od 16 časova", 10),
        new("dusan", "Gitara", 1800m, Lokacija.Uzivo, "Vikendom, po dogovoru", 19),

        new("tamara", "Engleski jezik", 1700m, Lokacija.Oboje, "Radnim danima od 10 do 18 časova", 13),
        new("tamara", "Srpski jezik i književnost", 1500m, Lokacija.Online, "Subotom od 9 do 13 časova", 24),

        new("bojan", "Matematika", 1900m, Lokacija.Oboje, "Radnim danima uveče", 16),
        new("bojan", "Baze podataka", 2400m, Lokacija.Online, "Vikendom posle 12 časova", 28),
    };

    /// <summary>
    /// Forty ocene over eight predavači — <c>dusan</c> and <c>tamara</c> are absent and stay
    /// unrated. The averages run from 2.00 to 4.75 so that sorting by prosečna ocena is
    /// visibly not the same order as sorting by broj ocena, which is what issue 14's "top
    /// predavači, never by average alone" needs in order to be demonstrable at all.
    /// </summary>
    private static readonly (string Predavac, int Pomeraj, int[] Vrednosti)[] Ocene =
    {
        ("ana", 0, new[] { 5, 5, 4, 5, 5, 4 }),
        ("marko", 2, new[] { 4, 4, 5, 3, 4 }),
        ("jelena", 4, new[] { 5, 5, 5, 4 }),
        ("stefan", 6, new[] { 3, 4, 3, 2, 3 }),
        ("milica", 1, new[] { 5, 4, 4, 5, 4, 5, 5 }),
        ("nikola", 3, new[] { 2, 3, 1, 2 }),
        ("ivana", 5, new[] { 4, 5, 4, 4, 5, 3 }),
        ("bojan", 7, new[] { 3, 2, 3 }),
    };

    /// <summary>
    /// A few marked predavači, so the Omiljeni screen has something on it. <c>milos</c> is the
    /// credential učenik and has three, one of them the unrated <c>tamara</c>.
    /// </summary>
    private static readonly (string Ucenik, string Predavac)[] Omiljeni =
    {
        ("milos", "ana"),
        ("milos", "jelena"),
        ("milos", "tamara"),
        ("sara", "ana"),
        ("sara", "milica"),
        ("luka", "marko"),
        ("luka", "nikola"),
        ("mina", "ivana"),
        ("petar", "dusan"),
    };
}
