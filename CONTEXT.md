# Privatni časovi

A directory where **predavači** publish oglasi for the private lessons they teach, and **učenici** search them, rate the predavači, and keep a list of favourites. The platform advertises and reviews; it does not book, schedule, or take payment.

The graded requirements this model serves are in [`docs/projektni-zadatak.md`](./docs/projektni-zadatak.md).

## Language

### Ljudi

**Korisnik**:
A registered account. Every korisnik is exactly one of predavač, učenik, or administrator, fixed when the account is created.
_Avoid_: Nalog, Član, Profil — as words for the korisnik themselves. „Profil“ is still the right name for the *screen* that shows one, which is what the spec calls it („Moj profil“, „Profil predavača“).

**Predavač**:
A korisnik who teaches privately, publishing their own oglasi and receiving ocene.
_Avoid_: Nastavnik, Profesor, Tutor, Instruktor

**Kontakt podaci**:
The telefon and email an učenik uses to reach a predavač directly, shown on an oglas and on the predavač's profile to any signed-in korisnik. Surfacing these is the whole purpose of the platform — it carries no messaging of its own.
_Avoid_: Kontakt info, Podaci za kontakt

**Učenik**:
A korisnik who searches oglasi, rates predavači, and keeps omiljeni.
_Avoid_: Student, Polaznik, Klijent, Đak

**Administrator**:
A korisnik who manages accounts, deactivates oglasi, and reads platform statistics. Not a participant in časovi.
_Avoid_: Admin, Moderator

### Ponuda

**Oglas**:
A predavač's public offer to teach **one** predmet, carrying a cena časa, a lokacija, and a dostupnost. A predavač who teaches three predmeti publishes three oglasi. It is what an učenik searches, and what a search returns. Visible only to signed-in korisnici. It can be deactivated — by its own predavač or by an administrator — after which it vanishes from search but is never erased.
_Avoid_: Ponuda, Objava, Kurs, Listing

**Predmet**:
The school subject an oglas teaches — matematika, fizika, engleski. Drawn from a fixed list a predavač chooses from and cannot add to.
_Avoid_: Tema, Kurs

**Oblast**:
The field a predmet belongs to — prirodne nauke, jezici, IT. Every predmet belongs to exactly one oblast, the same one for every predavač.
_Avoid_: Kategorija, Grupa, Sekcija

**Cena časa**:
What a single čas under a given oglas costs. It belongs to the oglas, not to the predavač — the same predavač may charge differently per oglas.
_Avoid_: Tarifa, Honorar, Naknada

**Lokacija**:
Whether a čas under an oglas is held online, uživo, or either way. The platform records no place beyond that — no city, no address. An učenik searching for one of the two also sees the oglasi open to both.
_Avoid_: Mesto, Grad, Tip nastave

**Dostupnost**:
When a predavač says they are free to teach under an oglas, written in their own words. Published for an učenik to read — the platform never reserves against it, searches it, or interprets it.
_Avoid_: Termin, Slot, Raspored, Slobodno vreme

**Čas**:
A single private lesson between one predavač and one učenik. The thing being advertised and priced — arranged between the two of them off the platform.
_Avoid_: Lekcija, Sesija, Sat, Rezervacija

### Ocenjivanje

**Ocena**:
An učenik's rating of a predavač, from 1 to 5 — a score and nothing else, with no written comment. An učenik has at most one ocena per predavač and may change it later. A predavač sees the scores they have been given, never who gave them.
_Avoid_: Recenzija, Komentar, Rejting, Glas

**Prosečna ocena**:
The mean of all ocene a predavač has received, shown on their profile alongside how many there are. A predavač nobody has rated has no prosečna ocena — not a zero — and sorts after every rated predavač.
_Avoid_: Rejting, Skor, Prosek

**Omiljeni**:
The predavači an učenik has marked to find again. Private to that učenik and invisible to the predavač.
_Avoid_: Favoriti, Sačuvani, Lista želja
