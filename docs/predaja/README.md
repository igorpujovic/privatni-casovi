# Uputstvo za pokretanje

Projekat je ASP.NET Core MVC aplikacija (.NET 8) sa MS SQL Server bazom.

## 1. Preduslovi

- **.NET 8 SDK** — projekat cilja `net8.0`. Ako je instaliran samo noviji SDK (9 ili 10),
  aplikacija se i dalje builduje, ali je najsigurnije imati .NET 8 runtime.
- **MS SQL Server** — bilo koja edicija od 2016 naviše (Express, Developer ili LocalDB).
  Skripta ne koristi konstrukcije specifične za noviju verziju.
- **Visual Studio 2022** (ili `dotnet` CLI).

## 2. Kreiranje baze

U SSMS-u otvoriti `privatni-casovi.sql` i pokrenuti ga (F5) **povezan na `master`**.
Skripta sama kreira bazu `PrivatniCasovi`, sve tabele i sve podatke.

Isto preko komandne linije:

```
sqlcmd -S <naziv-servera> -i privatni-casovi.sql
```

Skripta ne zadaje putanje do `.mdf`/`.ldf` fajlova, pa se baza kreira na podrazumevanoj
lokaciji tog SQL Server instance-a — radi na svakom računaru bez izmena.

Očekivano stanje posle izvršavanja: 5 oblasti, 15 predmeta, 25 oglasa, 40 ocena i
21 korisnik (10 predavača, 10 učenika, 1 administrator).

> **Tokom izvršavanja SQL Server ispisuje pet upozorenja** o maksimalnoj dužini ključa
> indeksa — na `PK_AspNetUserLogins`, `PK_AspNetUserRoles`, `PK_AspNetUserTokens`,
> `PK_Omiljeni` i `IX_Ocene_UcenikId_PredavacId`. To su upozorenja, ne greške, i skripta se
> izvršava do kraja.
>
> Razlog: EF Core Identity deklariše ključne kolone kao `nvarchar(450)`, što je 900 bajtova
> po koloni, pa dvočlani ključ formalno ide do 1800, a tročlani do 2700 bajtova. SQL Server
> računa to upozorenje iz deklaracije kolona, ne iz podataka. Stvarne vrednosti su GUID-ovi
> od 72 bajta, dakle 144 bajta po ključu — višestruko ispod granice. Ovo upozorenje se
> pojavljuje na svakoj EF Core Identity bazi.

## 3. Connection string

Aplikacija se podrazumevano povezuje na **LocalDB**. Podešavanje je u
`Privatni casovi/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=PrivatniCasovi;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

**Ako se baza ne nalazi na LocalDB**, izmeniti samo `Server=`:

| Okruženje | `Server=` |
|---|---|
| LocalDB (podrazumevano) | `(localdb)\mssqllocaldb` |
| SQL Server Express | `.\SQLEXPRESS` |
| Podrazumevani lokalni instance | `.` ili `localhost` |
| Imenovani / udaljeni server | `NAZIV-RACUNARA\INSTANCE` |

Za SQL Server autentifikaciju umesto Windows autentifikacije, `Trusted_Connection=True`
zameniti sa `User Id=<korisnik>;Password=<lozinka>`.

## 4. Pokretanje

U Visual Studiju otvoriti `Privatni casovi.slnx` i pokrenuti projekat (Ctrl+F5).
Podrazumevani profil je `http` (`http://localhost:5206`).

Profil `https` takođe postoji, ali zahteva da je razvojni sertifikat prihvaćen:

```
dotnet dev-certs https --trust
```

Preko komandne linije:

```
dotnet run --project "Privatni casovi"
```

## 5. Prijava

Pristupni podaci za sve tri uloge nalaze se u `pristupni-podaci.md`.

## Napomena o pokretanju bez skripte

Aplikacija pri startovanju sama primenjuje migracije i puni bazu podacima ako je prazna.
Zato se, ako se skripta iz tačke 2 preskoči, baza kreira automatski pri prvom pokretanju.
Oba koraka su zaštićena od dupliranja, pa pokretanje aplikacije nad bazom koja je već
kreirana skriptom ne menja podatke.
