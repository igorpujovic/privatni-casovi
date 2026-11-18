namespace Privatni_casovi.ViewModels;

/// <summary>
/// "Moje ocene" — the scores one predavač has been given, as that predavač reads them.
/// </summary>
/// <remarks>
/// <para>
/// The ocene are plain <c>int</c>s and that is the anonymity rule in the type system: there is
/// no <c>Ocena</c> here to carry a <c>UcenikId</c>, so no view on this screen can render who
/// gave what even by accident. <see cref="Repositories.IOcenaRepository.ZaPredavacaAsync"/>
/// drops the identity in the query, and this keeps it dropped.
/// </para>
/// <para>
/// <see cref="ProsecnaOcena"/> comes from the repository rather than from
/// <see cref="Ocene"/>.Average() so that the number on this page and the number on the profil
/// predavača have one definition between them, including the <c>null</c> an unrated predavač
/// gets instead of 0,00.
/// </para>
/// </remarks>
/// <param name="Ocene">
/// Every score this predavač has received, most recently left first. Editing an ocena keeps
/// its row, so this is the order they were first given in and not the order they last
/// changed — which nobody can tell apart here anyway, the scores carrying no dates.
/// </param>
/// <param name="ProsecnaOcena">Their average, or <c>null</c> if nobody has rated them yet.</param>
public record MojeOceneViewModel(IReadOnlyList<int> Ocene, decimal? ProsecnaOcena)
{
    /// <summary>
    /// How many ocene the average is of. Derived rather than passed: the list is the ocene, so
    /// a separate count could only ever disagree with it.
    /// </summary>
    public int BrojOcena => Ocene.Count;
}
