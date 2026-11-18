namespace Privatni_casovi.ViewModels;

/// <summary>
/// Counting things in Serbian on screen — "1 oglas", "3 oglasa", "6 ocena".
/// </summary>
/// <remarks>
/// Here once because the rule is small and easy to get subtly wrong, and every screen that
/// counts something needs it: the pretraga counts oglasi, the profil predavača counts ocene.
/// </remarks>
public static class Mnozina
{
    /// <summary>
    /// The count with the right form of the word after it.
    /// </summary>
    /// <remarks>
    /// Serbian takes three forms, and which one a number wants is decided by its last digit —
    /// except in the teens, where 11 through 14 all take the third. That exception is the
    /// whole reason this is a function and not a <c>broj % 10</c> at each call site.
    /// </remarks>
    /// <param name="jedan">The form after 1, 21, 31 — "oglas", "ocena".</param>
    /// <param name="nekoliko">The form after 2, 3, 4 — "oglasa", "ocene".</param>
    /// <param name="mnogo">The form after 5 and up, and after the teens — "oglasa", "ocena".</param>
    public static string Broj(int broj, string jedan, string nekoliko, string mnogo)
        => $"{broj} {Rec(broj, jedan, nekoliko, mnogo)}";

    private static string Rec(int broj, string jedan, string nekoliko, string mnogo)
    {
        if (broj % 100 is >= 11 and <= 14)
        {
            return mnogo;
        }

        return (broj % 10) switch
        {
            1 => jedan,
            2 or 3 or 4 => nekoliko,
            _ => mnogo
        };
    }
}
