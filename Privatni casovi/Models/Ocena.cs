namespace Privatni_casovi.Models;

/// <summary>
/// An učenik's rating of a predavač, from 1 to 5 — a score and nothing else, with no
/// written comment.
/// </summary>
/// <remarks>
/// An učenik has at most one ocena per predavač (unique index on the pair) and may change
/// it later. A predavač sees the scores they have been given, never who gave them, so
/// <see cref="UcenikId"/> must never reach a view a predavač can open.
/// </remarks>
public class Ocena
{
    public int Id { get; set; }

    public string UcenikId { get; set; } = string.Empty;

    public Korisnik? Ucenik { get; set; }

    public string PredavacId { get; set; } = string.Empty;

    public Korisnik? Predavac { get; set; }

    /// <summary>1 to 5, held to that range by a check constraint on the table.</summary>
    public int Vrednost { get; set; }
}
