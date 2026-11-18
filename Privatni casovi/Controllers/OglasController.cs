using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Repositories;

namespace Privatni_casovi.Controllers;

/// <summary>
/// One oglas as every signed-in korisnik reads it — the page a pretraga card leads to, and
/// the one that carries the predavač's kontakt podaci.
/// </summary>
/// <remarks>
/// <para>
/// This is the point of the application. There is no booking and no messaging, so this page
/// is where an učenik gets the email and telefon they need to arrange a čas; everything else
/// on it is what they decide with before they write.
/// </para>
/// <para>
/// Open to all three uloge, like the pretraga it is reached from: an administrator and a
/// predavač read the same page an učenik does. There is no <c>[Authorize]</c> because the
/// fallback policy in <c>Program.cs</c> already requires a signed-in korisnik everywhere that
/// does not say <c>[AllowAnonymous]</c> — and kontakt podaci are exactly what must not be
/// readable by an anonymous visitor.
/// </para>
/// <para>
/// A predavač's own oglasi are <see cref="MojiOglasiController"/>, which is a different screen
/// with a different audience: it lists withdrawn oglasi and it edits them. Nothing here writes.
/// </para>
/// </remarks>
public class OglasController : Controller
{
    private readonly IOglasRepository _oglasi;

    public OglasController(IOglasRepository oglasi)
    {
        _oglasi = oglasi;
    }

    /// <summary>
    /// The detalji of one visible oglas, with the standing and the kontakt podaci of the
    /// predavač who published it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IOglasRepository.AktivanAsync"/> is the whole of the visibility rule: it
    /// returns <c>null</c> for an oglas that does not exist, one that was withdrawn, and one
    /// whose predavač is blocked. An učenik who kept a link to an oglas that has since been
    /// taken down gets the same answer as one who typed an id at random, which is the point —
    /// a withdrawn oglas is out of sight (ADR-0001), not merely out of search, and its
    /// predavač's telefon goes with it.
    /// </para>
    /// <para>
    /// It is also one query for the page: the oglas arrives with its predmet, that predmet's
    /// oblast and its predavač loaded, and the prosečna ocena computed alongside — the same
    /// <see cref="OglasSaOcenom"/> a pretraga card is rendered from, so the two screens cannot
    /// disagree about a predavač's standing.
    /// </para>
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Detalji(int id)
    {
        var rezultat = await _oglasi.AktivanAsync(id);

        if (rezultat is null)
        {
            return NotFound();
        }

        return View(rezultat);
    }
}
