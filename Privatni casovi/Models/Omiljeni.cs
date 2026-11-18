namespace Privatni_casovi.Models;

/// <summary>
/// A predavač an učenik has marked to find again. Private to that učenik and invisible to
/// the predavač.
/// </summary>
/// <remarks>The pair is the primary key — marking the same predavač twice is not a thing.</remarks>
public class Omiljeni
{
    public string UcenikId { get; set; } = string.Empty;

    public Korisnik? Ucenik { get; set; }

    public string PredavacId { get; set; } = string.Empty;

    public Korisnik? Predavac { get; set; }
}
