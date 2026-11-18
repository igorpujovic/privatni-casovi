using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// The public profil of one predavač — the screen an učenik lands on from an oglas to see
/// everything else that predavač teaches, read their biografija, and decide whether to write.
/// </summary>
/// <remarks>
/// <para>
/// Open to all three uloge, like the oglas detail page it is reached from, and for the same
/// reason there is no <c>[Authorize]</c>: the fallback policy in <c>Program.cs</c> already
/// requires a signed-in korisnik everywhere that does not say <c>[AllowAnonymous]</c>, and
/// the kontakt podaci on this page are exactly what an anonymous visitor must not read.
/// </para>
/// <para>
/// <see cref="ProfilController"/> is the other profile screen and is a different thing: it is
/// a predavač editing their own, behind <c>[Authorize(Roles = Uloge.Predavac)]</c>, with no id
/// anywhere on it. This one takes an id, shows somebody else's, and writes nothing. The two
/// names come from the spec's own Screens table — "Moj profil" and "Profil predavača".
/// </para>
/// <para>
/// The ocenjivanje form (issue 11) and the Omiljeni button (issue 12) are markup on this
/// screen that post to <see cref="OcenaController"/> and <see cref="OmiljeniController"/>,
/// which is what keeps the <c>[Authorize(Roles = Uloge.Ucenik)]</c> those two need off a page
/// every korisnik reads. This controller still writes nothing — it reads the omiljeni list to
/// decide which way the button faces, and that is all.
/// </para>
/// </remarks>
public class PredavacController : Controller
{
    private readonly IKorisnikRepository _korisnici;
    private readonly IOglasRepository _oglasi;
    private readonly IOcenaRepository _ocene;
    private readonly IOmiljeniRepository _omiljeni;
    private readonly UserManager<Korisnik> _userManager;

    public PredavacController(
        IKorisnikRepository korisnici,
        IOglasRepository oglasi,
        IOcenaRepository ocene,
        IOmiljeniRepository omiljeni,
        UserManager<Korisnik> userManager)
    {
        _korisnici = korisnici;
        _oglasi = oglasi;
        _ocene = ocene;
        _omiljeni = omiljeni;
        _userManager = userManager;
    }

    /// <summary>
    /// One predavač's profil: foto, biografija, kontakt podaci, prosečna ocena with how many
    /// ocene it is of, and their oglasi.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IKorisnikRepository.PredavacAsync"/> is the whole of the visibility rule and
    /// is asked first: it answers <c>null</c> for an id that is nobody — including no id at
    /// all, since the route makes it optional — for an učenik or an administrator, and for a
    /// blocked predavač. All of them 404 identically, which is the point: a blocked predavač's
    /// telefon must not be reachable by typing their id, and an učenik who kept a link to one
    /// gets the same answer as somebody guessing. The two writes that take the same id,
    /// <c>OcenaController.Oceni</c> and <see cref="OmiljeniController"/>, resolve through that
    /// same method for the same reason.
    /// </para>
    /// <para>
    /// The standing comes from <see cref="IOcenaRepository"/> and the oglasi come back plain,
    /// which is the one read on the platform that does not want a
    /// <see cref="OglasSaOcenom"/>: every row would carry the same predavač's same two
    /// numbers, and a predavač with no active oglasi — who this page still has to render —
    /// would have no row to carry them at all. The profil is about the person; the oglasi are
    /// a list on it.
    /// </para>
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Profil(string? id)
    {
        var predavac = await _korisnici.PredavacAsync(id);

        if (predavac is null)
        {
            return NotFound();
        }

        return View(new ProfilPredavacaViewModel(
            predavac,
            await _ocene.ProsecnaOcenaAsync(predavac.Id),
            await _ocene.BrojOcenaAsync(predavac.Id),
            await _oglasi.AktivniZaPredavacaAsync(predavac.Id),
            await MojaOcenaAsync(predavac.Id),
            await JeOmiljeniAsync(predavac.Id)));
    }

    /// <summary>
    /// The score the signed-in korisnik has already given this predavač, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The role is checked before the query rather than after, so the two korisnici who cannot
    /// rate cost nothing to render: a predavač or an administrator reading this page has no
    /// ocena by construction, and the form they would fill in is not on their copy of it.
    /// </remarks>
    private async Task<int?> MojaOcenaAsync(string predavacId)
    {
        if (!User.IsInRole(Uloge.Ucenik))
        {
            return null;
        }

        var ucenikId = _userManager.GetUserId(User)!;
        return (await _ocene.PostojecaAsync(ucenikId, predavacId))?.Vrednost;
    }

    /// <summary>
    /// Whether this predavač is already on the signed-in učenik's list — which way the
    /// Omiljeni button faces.
    /// </summary>
    /// <remarks>
    /// The same role check as <see cref="MojaOcenaAsync"/>, and the same reason: only an
    /// učenik has omiljeni, so a predavač reading this page is <c>false</c> without a query,
    /// and the button they would press is not on their copy of it. That <c>false</c> is not a
    /// leak either — this is the one thing on the profil the predavač it belongs to must not
    /// learn, and what they are told is what everybody who is not an učenik is told.
    /// </remarks>
    private async Task<bool> JeOmiljeniAsync(string predavacId)
    {
        if (!User.IsInRole(Uloge.Ucenik))
        {
            return false;
        }

        return await _omiljeni.PostojiAsync(_userManager.GetUserId(User)!, predavacId);
    }
}
