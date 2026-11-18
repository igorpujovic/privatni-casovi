using System.ComponentModel.DataAnnotations;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// The ocenjivanje form on the profil predavača — one score, and which predavač it is for.
/// </summary>
/// <remarks>
/// <para>
/// There is no <c>UcenikId</c> here on purpose, which is spec rule 5 doing its job: the
/// učenik leaving the ocena is <c>User</c>, read on the server, so a crafted POST cannot rate
/// a predavač in somebody else's name. <see cref="PredavacId"/> is bound because it is the
/// target rather than the author — an učenik is allowed to rate any predavač, so the request
/// naming one is not a claim about who is signed in.
/// </para>
/// <para>
/// There is no <c>Id</c> either. Whether a POST creates an ocena or edits one is not the
/// browser's to say: the controller looks the pair up and decides, because an učenik has at
/// most one ocena per predavač and the second POST has to land on the first one's row.
/// </para>
/// </remarks>
public class OcenaFormViewModel
{
    /// <summary>Said when the form comes back with the "Izaberite…" placeholder still on it.</summary>
    /// <remarks>
    /// A constant because two things say it: the <c>[Required]</c> below, and
    /// <c>OcenaController</c>, which decides the alert text from the bound model rather than
    /// lifting it out of <c>ModelState</c>. A <c>Vrednost</c> that is not a number at all
    /// fails in the binder before any attribute here runs, so <c>ModelState</c> would answer
    /// with the binder's „mora biti broj“ — true, and beside the point on a form that is a
    /// closed <c>&lt;select&gt;</c> nobody typed a number into.
    /// </remarks>
    public const string NijeIzabrana = "Izaberite ocenu.";

    /// <summary>Said when a score arrives that was never one of the six options on the form.</summary>
    public const string VanOpsega = "Ocena mora biti ceo broj od 1 do 5.";

    /// <summary>
    /// The predavač being rated. Checked against
    /// <see cref="Repositories.IKorisnikRepository.PredavacAsync"/> before anything is
    /// written — an id that is not a predavač, or is a blocked one, is refused there.
    /// </summary>
    [Required]
    public string PredavacId { get; set; } = string.Empty;

    /// <summary>
    /// The score, 1 to 5.
    /// </summary>
    /// <remarks>
    /// Nullable so that submitting the form without picking anything is a validation error
    /// rather than a silent 0 — and the <c>&lt;select&gt;</c> opens on a placeholder rather
    /// than pre-selecting a score the učenik never chose. The <c>[Range]</c> is the
    /// user-facing half of the <c>CK_Ocena_Vrednost</c> check constraint on the table: without
    /// it a hand-crafted <c>Vrednost=9</c> surfaces as a <c>DbUpdateException</c> and a 500.
    /// </remarks>
    [Required(ErrorMessage = NijeIzabrana)]
    [Range(1, 5, ErrorMessage = VanOpsega)]
    public int? Vrednost { get; set; }
}
