using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// "Moje ocene" — a predavač reading the scores they have been given, without ever being told
/// who gave them.
/// </summary>
/// <remarks>
/// <para>
/// Split off from <see cref="OcenaController"/> rather than added to it, and the reason is the
/// same one that split <see cref="MojiOglasiController"/> from <see cref="OglasController"/>:
/// an action-level <c>[Authorize]</c> cannot loosen a role the class requires, so a screen for
/// predavači cannot live on a controller that is <c>[Authorize(Roles = Uloge.Ucenik)]</c> at
/// the class level. Two roles, two controllers.
/// </para>
/// <para>
/// There is no id on this controller, which is the whole of its security story — the predavač
/// whose ocene are read is <c>User</c>. Anonymity is not enforced here either: it is enforced
/// by <see cref="IOcenaRepository.ZaPredavacaAsync"/> returning bare <c>int</c>s, so this
/// controller has nothing to leak even if the view asked.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Predavac)]
public class MojeOceneController : Controller
{
    private readonly IOcenaRepository _ocene;
    private readonly UserManager<Korisnik> _userManager;

    public MojeOceneController(IOcenaRepository ocene, UserManager<Korisnik> userManager)
    {
        _ocene = ocene;
        _userManager = userManager;
    }

    /// <summary>
    /// Every ocena this predavač has received, newest first, with the prosek they add up to.
    /// </summary>
    /// <remarks>
    /// Two reads rather than averaging the list in memory: the prosečna ocena has one
    /// definition on the platform, and it is the repository's — including the <c>null</c> that
    /// makes an unrated predavač read "Nema ocena" here exactly as it does on their profil.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Never null: the class requires the Predavac role, so the cookie has a korisnik.
        var predavacId = _userManager.GetUserId(User)!;

        return View(new MojeOceneViewModel(
            await _ocene.ZaPredavacaAsync(predavacId),
            await _ocene.ProsecnaOcenaAsync(predavacId)));
    }
}
