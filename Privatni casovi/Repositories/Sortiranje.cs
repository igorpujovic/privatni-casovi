namespace Privatni_casovi.Repositories;

/// <summary>
/// The five sort orders <see cref="IOglasRepository.PretragaAsync"/> understands. They are
/// <c>const</c> strings because they travel through the query string of the pretraga form,
/// so the sort survives filtering and paging.
/// </summary>
/// <remarks>
/// Anything else — including <c>null</c> — sorts by <see cref="Najnoviji"/>. A misspelt sort
/// in a hand-typed URL should show results, not an error.
/// </remarks>
public static class Sortiranje
{
    public const string CenaRastuce = "cena_asc";

    public const string CenaOpadajuce = "cena_desc";

    /// <summary>By prosečna ocena. A predavač nobody has rated sorts last, never as a zero.</summary>
    public const string OcenaOpadajuce = "ocena_desc";

    public const string BrojOcenaOpadajuce = "broj_ocena_desc";

    /// <summary>By <c>DatumKreiranja</c>, newest first. The default.</summary>
    public const string Najnoviji = "najnoviji";

    /// <summary>
    /// The given sort if it is one of the five, otherwise <see cref="Najnoviji"/>. A misspelt
    /// sort in a hand-typed URL should show results, not an error — and should come back
    /// spelled as one of the five, so the form that renders it has something to select.
    /// </summary>
    public static string Prepoznat(string? sortBy)
        => sortBy switch
        {
            CenaRastuce or CenaOpadajuce or OcenaOpadajuce or BrojOcenaOpadajuce => sortBy,
            _ => Najnoviji
        };
}
