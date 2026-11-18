using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;
using Privatni_casovi.ViewModels;

namespace Privatni_casovi.Controllers;

/// <summary>
/// "Moji oglasi" — a predavač publishing, editing and withdrawing their own oglasi. Every
/// action here works on the signed-in predavač's oglasi and no one else's.
/// </summary>
/// <remarks>
/// <para>
/// Two rules run through all of it. The predavač is taken from <c>User</c> and never from the
/// request body — see <see cref="OglasFormViewModel"/>, which has no <c>PredavacId</c> to
/// post. And an oglas is withdrawn, never deleted (ADR-0001), by its owner here and by an
/// administrator in issue 13; there is no delete action on this controller because there is
/// no delete anywhere.
/// </para>
/// <para>
/// The role check is on the class, so an učenik or an administrator reaching any of these URLs
/// is refused before an action runs. Owning the oglas is a separate question the role cannot
/// answer, and <see cref="MojAsync"/> answers it on every single action that names an id.
/// </para>
/// <para>
/// The name is qualified — this is the screen the spec calls "Moji oglasi" — because the
/// unqualified one belongs to <see cref="OglasController"/>, the oglas as every signed-in
/// korisnik sees it. Keeping that class-level role attribute is what forced the split: an
/// action-level <c>[Authorize]</c> cannot loosen a role required by the class, so the page an
/// učenik reads could not have lived here whatever it was called.
/// </para>
/// </remarks>
[Authorize(Roles = Uloge.Predavac)]
public class MojiOglasiController : Controller
{
    private readonly IOglasRepository _oglasi;
    private readonly IPredmetRepository _predmeti;
    private readonly UserManager<Korisnik> _userManager;

    public MojiOglasiController(
        IOglasRepository oglasi,
        IPredmetRepository predmeti,
        UserManager<Korisnik> userManager)
    {
        _oglasi = oglasi;
        _predmeti = predmeti;
        _userManager = userManager;
    }

    /// <summary>
    /// Everything this predavač has ever published, withdrawn oglasi included.
    /// </summary>
    /// <remarks>
    /// The one listing in the application that shows deactivated oglasi to a non-administrator,
    /// and it has to: this is where their owner brings one back.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await _oglasi.SviZaPredavacaAsync(PredavacId));

    [HttpGet]
    public async Task<IActionResult> Create()
        => View(await SaPredmetimaAsync(new OglasFormViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OglasFormViewModel model)
    {
        if (!await JeStvaranPredmetAsync(model) || !ModelState.IsValid)
        {
            return View(await SaPredmetimaAsync(model));
        }

        // PredavacId comes from the cookie, DatumKreiranja from the clock and Aktivan from
        // the fact that publishing is what this action does. None of the three is bound.
        var oglas = new Oglas
        {
            PredavacId = PredavacId,
            DatumKreiranja = DateTime.Now,
            Aktivan = true
        };

        Prepisi(model, oglas);

        _oglasi.Add(oglas);
        await _oglasi.SaveChangesAsync();

        TempData["Poruka"] = "Oglas je objavljen.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var oglas = await MojAsync(id);

        if (oglas is null)
        {
            return NijeMoj();
        }

        return View(await SaPredmetimaAsync(new OglasFormViewModel
        {
            Id = oglas.Id,
            PredmetId = oglas.PredmetId,
            CenaCasa = oglas.CenaCasa,
            Lokacija = oglas.Lokacija,
            Dostupnost = oglas.Dostupnost
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OglasFormViewModel model)
    {
        // Ownership is re-checked here rather than inherited from the GET: the form that
        // produced this POST is not evidence of anything, and the id in the route is what is
        // about to be written.
        var oglas = await MojAsync(id);

        if (oglas is null)
        {
            return NijeMoj();
        }

        if (!await JeStvaranPredmetAsync(model) || !ModelState.IsValid)
        {
            model.Id = id;
            return View(await SaPredmetimaAsync(model));
        }

        // Only the four fields the form owns are written. DatumKreiranja stays what it was and
        // Aktivan stays what Deaktiviraj or Aktiviraj last made it — editing an oglas that was
        // withdrawn must not quietly republish it.
        Prepisi(model, oglas);

        _oglasi.Update(oglas);
        await _oglasi.SaveChangesAsync();

        TempData["Poruka"] = "Oglas je izmenjen.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Withdraws one of this predavač's oglasi from search. Nothing is deleted.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deaktiviraj(int id)
        => PostaviAktivnostAsync(id, aktivan: false, "Oglas je povučen.");

    /// <summary>Puts a withdrawn oglas back into search.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Aktiviraj(int id)
        => PostaviAktivnostAsync(id, aktivan: true, "Oglas je ponovo objavljen.");

    /// <summary>The signed-in predavač. Never <c>null</c>: the class requires their role.</summary>
    private string PredavacId => _userManager.GetUserId(User)!;

    /// <summary>
    /// The oglas with this id <i>if it belongs to the signed-in predavač</i>, otherwise
    /// <c>null</c> — whether because there is no such oglas or because it is somebody else's.
    /// Every action that takes an id goes through here before it reads or writes anything.
    /// </summary>
    /// <remarks>
    /// The lookup is deliberately the unfiltered <see cref="IRepository{T}.GetByIdAsync"/>: a
    /// predavač editing or reactivating an oglas they withdrew needs the row the visible-only
    /// reads hide.
    /// </remarks>
    private async Task<Oglas?> MojAsync(int id)
    {
        var oglas = await _oglasi.GetByIdAsync(id);
        return oglas is not null && oglas.PredavacId == PredavacId ? oglas : null;
    }

    /// <summary>The answer to an id that is not this predavač's: 403, never the form.</summary>
    /// <remarks>
    /// <para>
    /// Somebody else's oglas and an id that does not exist get the same answer on purpose.
    /// Telling the two apart would let a predavač walk the id space and count a competitor's
    /// oglasi, including the ones they withdrew from search.
    /// </para>
    /// <para>
    /// <c>Forbid</c> is the framework's 403 for a request that <i>is</i> signed in: the cookie
    /// handler turns it into the AccessDenied page, which is the answer a signed-in predavač
    /// should read rather than a bare status code.
    /// </para>
    /// </remarks>
    private IActionResult NijeMoj() => Forbid();

    private async Task<IActionResult> PostaviAktivnostAsync(int id, bool aktivan, string poruka)
    {
        if (await MojAsync(id) is null)
        {
            return NijeMoj();
        }

        // Ownership is settled, so the repository's own flag flip does the write.
        await (aktivan ? _oglasi.AktivirajAsync(id) : _oglasi.DeaktivirajAsync(id));

        TempData["Poruka"] = poruka;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Copies the four fields the form owns onto an oglas, and nothing else.</summary>
    /// <remarks>
    /// Reached only once <see cref="ModelState"/> is valid, so the nullable properties are
    /// answered.
    /// </remarks>
    private static void Prepisi(OglasFormViewModel model, Oglas oglas)
    {
        oglas.PredmetId = model.PredmetId!.Value;
        oglas.CenaCasa = model.CenaCasa!.Value;
        oglas.Lokacija = model.Lokacija!.Value;
        oglas.Dostupnost = model.Dostupnost;
    }

    /// <summary>
    /// Whether the posted predmet is one of the seeded ones, recorded on
    /// <see cref="ModelState"/> if it is not.
    /// </summary>
    /// <remarks>
    /// <c>[Required]</c> only says the dropdown was answered. Predmeti are a closed list the
    /// form does not let anybody add to, but the id still arrives in the request body: without
    /// this, the answer to a hand-crafted <c>PredmetId=9999</c> is a foreign-key
    /// <c>DbUpdateException</c> and a 500 rather than the form asking for a predmet.
    /// </remarks>
    private async Task<bool> JeStvaranPredmetAsync(OglasFormViewModel model)
    {
        if (model.PredmetId is null || await _predmeti.GetByIdAsync(model.PredmetId.Value) is not null)
        {
            return true;
        }

        ModelState.AddModelError(nameof(OglasFormViewModel.PredmetId), "Izabrani predmet ne postoji.");
        return false;
    }

    /// <summary>
    /// Fills the predmet dropdown. Called before every render of the form — a POST that comes
    /// back invalid re-renders the same <c>&lt;select&gt;</c>, and its contents did not travel
    /// with the request.
    /// </summary>
    private async Task<OglasFormViewModel> SaPredmetimaAsync(OglasFormViewModel model)
    {
        model.Oblasti = await _predmeti.OblastiSaPredmetimaAsync();
        return model;
    }
}
