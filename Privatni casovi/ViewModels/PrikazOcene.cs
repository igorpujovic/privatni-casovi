namespace Privatni_casovi.ViewModels;

/// <summary>
/// How a predavač's standing is spelled on screen — one place, because the pretraga cards,
/// the oglas detail page and the profil predavača all show the same two numbers and must
/// show them the same way.
/// </summary>
/// <remarks>
/// The rule this exists to hold is that <b>an unrated predavač has no prosečna ocena</b>: a
/// <c>null</c> average reads "Nema ocena" and never 0.00, which would say "rated badly"
/// rather than "not rated yet".
/// </remarks>
public static class PrikazOcene
{
    /// <summary>What a predavač nobody has rated shows instead of a number.</summary>
    public const string Nema = "Nema ocena";

    /// <summary>
    /// The average and how many ocene it is the average of — "4,67 (6 ocena)" — or
    /// <see cref="Nema"/>.
    /// </summary>
    /// <remarks>
    /// The count travels with the average because the average alone says little: a 5,00 from
    /// one učenik is not a 5,00 from twenty.
    /// </remarks>
    public static string Tekst(decimal? prosecnaOcena, int brojOcena)
        => prosecnaOcena is null
            ? Nema
            : $"{Prosek(prosecnaOcena.Value)} ({Broj(brojOcena)})";

    /// <summary>The average on its own — "4,67".</summary>
    /// <remarks>
    /// Split out of <see cref="Tekst"/> for "Moje ocene", which shows the same two numbers
    /// stacked rather than on one line and so cannot use the sentence. It is the same helper
    /// either way, which is the point: the format string lives here and not in a view.
    /// </remarks>
    public static string Prosek(decimal prosecnaOcena)
        => prosecnaOcena.ToString("0.00");

    /// <summary>How many ocene, in words — "6 ocena", "1 ocena", "2 ocene".</summary>
    /// <remarks>
    /// The three Serbian plural forms of "ocena" are picked once, here, rather than spelled
    /// out at each call to <see cref="Mnozina"/> — a screen that got the triple wrong would
    /// be wrong only on that screen, which is exactly the drift this class exists to stop.
    /// </remarks>
    public static string Broj(int brojOcena)
        => Mnozina.Broj(brojOcena, "ocena", "ocene", "ocena");

    /// <summary>
    /// The Bootstrap classes the line is rendered in — <see cref="Nema"/> reads as muted,
    /// a real average does not.
    /// </summary>
    /// <remarks>
    /// Here rather than repeated as a ternary in each view for the same reason
    /// <see cref="Tekst"/> is: "shown the same way" is as much the greying as the wording,
    /// and a screen that centralised one and inlined the other would still drift. The
    /// pretraga card and the oglas detail page call this; the profil predavača makes three.
    /// </remarks>
    public static string Klasa(decimal? prosecnaOcena)
        => prosecnaOcena is null ? "small text-muted" : "small";
}
