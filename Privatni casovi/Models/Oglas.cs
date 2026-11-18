namespace Privatni_casovi.Models;

/// <summary>
/// A predavač's public offer to teach <b>one</b> predmet, carrying a cena časa, a lokacija
/// and a dostupnost. A predavač who teaches three predmeti publishes three oglasi.
/// </summary>
/// <remarks>
/// Search returns oglasi, not predavači — a predavač with three matching oglasi appears
/// three times. Deactivating one hides it from search; see ADR-0001, nothing is erased.
/// </remarks>
public class Oglas
{
    public int Id { get; set; }

    public string PredavacId { get; set; } = string.Empty;

    public Korisnik? Predavac { get; set; }

    public int PredmetId { get; set; }

    public Predmet? Predmet { get; set; }

    /// <summary>
    /// What a single čas under this oglas costs. It belongs to the oglas, not to the
    /// predavač — the same predavač may charge differently per oglas.
    /// </summary>
    public decimal CenaCasa { get; set; }

    public Lokacija Lokacija { get; set; }

    /// <summary>
    /// When the predavač says they are free to teach, in their own words. Published for an
    /// učenik to read — never reserved against, searched, or interpreted.
    /// </summary>
    public string Dostupnost { get; set; } = string.Empty;

    public DateTime DatumKreiranja { get; set; }

    /// <summary>
    /// Nothing is deleted — a withdrawn oglas is deactivated, by its own predavač or by an
    /// administrator. See ADR-0001.
    /// </summary>
    public bool Aktivan { get; set; } = true;
}
