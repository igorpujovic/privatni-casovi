namespace Privatni_casovi.Models;

/// <summary>
/// The field a predmet belongs to — prirodne nauke, jezici, IT. A seeded, closed list:
/// nothing in the application creates one.
/// </summary>
public class Oblast
{
    public int Id { get; set; }

    public string Naziv { get; set; } = string.Empty;

    public ICollection<Predmet> Predmeti { get; set; } = new List<Predmet>();
}
