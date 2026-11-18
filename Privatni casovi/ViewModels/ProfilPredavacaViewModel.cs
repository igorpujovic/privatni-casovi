using Privatni_casovi.Models;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// "Profil predavača" — one predavač as every signed-in korisnik reads them: who they are,
/// how to reach them, how they stand, and everything they currently teach.
/// </summary>
/// <remarks>
/// <para>
/// A view model of its own rather than a repository type, which is the opposite call from the
/// oglas detail page. That screen is one row and renders straight off
/// <see cref="Repositories.OglasSaOcenom"/>; this one composes five reads that no single query
/// returns together, and the alternative would be five <c>ViewData</c> entries.
/// </para>
/// <para>
/// It is not a form and binds nothing, so spec rule 5 has no opinion on it — a record with no
/// setters is what a screen that only displays needs. The ocenjivanje form and the Omiljeni
/// button on this screen post their own shapes to their own controllers and do not bind this;
/// what they take from here is <see cref="MojaOcena"/> and <see cref="JeOmiljeni"/>, the state
/// each of them opens on.
/// </para>
/// </remarks>
/// <param name="Predavac">
/// Known to be a predavač and known not to be blocked — <see cref="Repositories.IKorisnikRepository.PredavacAsync"/>
/// answers <c>null</c> for anyone else, and this screen 404s on that.
/// </param>
/// <param name="ProsecnaOcena">
/// <c>null</c> when nobody has rated them — rendered "Nema ocena" by <see cref="PrikazOcene"/>
/// and never 0,00.
/// </param>
/// <param name="BrojOcena">How many ocene that average is of. Shown beside it, never alone.</param>
/// <param name="Oglasi">
/// Their oglasi an učenik can see, newest first. Withdrawn ones are not here — a predavač
/// manages those from "Moji oglasi", which is the one screen that shows them.
/// </param>
/// <param name="MojaOcena">
/// The score the signed-in učenik has already given this predavač, or <c>null</c> — either
/// because they have not rated them yet or because the reader is not an učenik at all and
/// sees no form. It is what makes the form come back pre-filled, which is what tells an
/// učenik that submitting it again changes their ocena rather than adding a second one.
/// </param>
/// <param name="JeOmiljeni">
/// Whether this predavač is already on the signed-in učenik's list — the state of the toggle,
/// deciding whether the button adds or removes. <c>false</c> for a reader who is not an
/// učenik, who has no list and no button; a predavač reading their own profil is told nothing
/// else, because who marked them is the one thing on this screen they must never learn.
/// </param>
public record ProfilPredavacaViewModel(
    Korisnik Predavac,
    decimal? ProsecnaOcena,
    int BrojOcena,
    IReadOnlyList<Oglas> Oglasi,
    int? MojaOcena,
    bool JeOmiljeni);
