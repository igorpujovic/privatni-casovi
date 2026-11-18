using Privatni_casovi.Models;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// One row of the administrator's korisnici listing: a korisnik, the uloga they hold, and
/// whether they are the administrator reading the screen.
/// </summary>
/// <remarks>
/// <para>
/// The uloga rides along because it is not on <see cref="Korisnik"/> — it is Identity role
/// membership, two tables away. Asking <c>UserManager.GetRolesAsync</c> per row would be a
/// query per korisnik, so <see cref="Repositories.IKorisnikRepository.UlogePoKorisnikuAsync"/>
/// reads all of them in one and the controller pairs them up here.
/// </para>
/// <para>
/// This screen has no form and this record binds nothing — the two writes on it post a bare
/// id to <c>AdminController</c>. What they take from here is <see cref="JeJa"/>: an
/// administrator must not be able to block their own account, and this is the flag that keeps
/// the button off their own row. It is not the enforcement, which is in the action; a button
/// that is not rendered is still a URL that can be posted to.
/// </para>
/// </remarks>
/// <param name="Uloga">
/// The role string — one of the <see cref="Uloge"/> constants. Empty only for a korisnik in no
/// role at all, which registration cannot produce.
/// </param>
/// <param name="JeJa">
/// Whether this row is the signed-in administrator. Their own row is listed like every other,
/// because a listing that hides you is a listing you cannot trust — it just has no button.
/// </param>
public record AdminKorisnikViewModel(Korisnik Korisnik, string Uloga, bool JeJa)
{
    /// <summary>
    /// The uloga as it is spelled on screen. The role constants carry no diacritics, because
    /// they are also claim values and route-safe strings; a korisnik reads "Predavač".
    /// </summary>
    public string UlogaNaziv => Uloga switch
    {
        Uloge.Predavac => "Predavač",
        Uloge.Ucenik => "Učenik",
        Uloge.Administrator => "Administrator",
        _ => Uloga
    };
}
