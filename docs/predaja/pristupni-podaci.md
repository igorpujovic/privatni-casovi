# Pristupni podaci

Uz projekat se, prema tačci 5 Tehničkih zahteva, obavezno predaju parametri za pristup
(username i password) za sve tri korisničke uloge.

Prijava ide **email adresom i lozinkom**. Sve tri lozinke su iste.

| Uloga | Username (email) | Password |
|---|---|---|
| Predavač | `ana.jovanovic@primer.rs` | `lozinka123` |
| Učenik | `milos.petrovic@primer.rs` | `lozinka123` |
| Administrator | `admin@privatnicasovi.rs` | `lozinka123` |

Puna imena naloga: Ana Jovanović (predavač), Miloš Petrović (učenik),
Vladimir Simonović (administrator).

## Napomene za pregled

- **Administrator se ne registruje** kroz aplikaciju — nalog postoji samo kroz seed.
- Nalog predavača *Ana Jovanović* je izabran namerno: ima tri oglasa (od kojih je jedan
  deaktiviran) i šest ocena, pa su i „Moji oglasi" i „Moje ocene" popunjeni ekrani.
- Ostali seed korisnici koriste istu lozinku, ako zatreba više naloga za probu.

Izvor istine za ove vrednosti su konstante `Lozinka`, `EmailPredavaca`, `EmailUcenika` i
`EmailAdministratora` u `Privatni casovi/Data/DbInitializer.cs`. Ako se seed menja, ovaj
fajl se menja s njim.
