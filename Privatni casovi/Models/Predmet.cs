namespace Privatni_casovi.Models;

/// <summary>
/// The school subject an oglas teaches — matematika, fizika, engleski. A seeded, closed
/// list a predavač chooses from and cannot add to.
/// </summary>
public class Predmet
{
    public int Id { get; set; }

    public string Naziv { get; set; } = string.Empty;

    /// <summary>Every predmet belongs to exactly one oblast, the same one for every predavač.</summary>
    public int OblastId { get; set; }

    public Oblast? Oblast { get; set; }

    public ICollection<Oglas> Oglasi { get; set; } = new List<Oglas>();
}
