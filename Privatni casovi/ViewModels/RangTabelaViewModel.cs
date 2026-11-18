using Privatni_casovi.Repositories;

namespace Privatni_casovi.ViewModels;

/// <summary>
/// One „sa najviše oglasa“ table on the statistika screen — the rows, and the three strings
/// that say what they are.
/// </summary>
/// <remarks>
/// <para>
/// Exists so <c>Views/Admin/_RangTabela.cshtml</c> can render the predmeti table and the
/// oblasti table from one piece of markup. The two are the same question asked one level
/// apart — <see cref="StavkaStatistike"/> is already the row type for both — and the only
/// things that differ between them are on this record.
/// </para>
/// <para>
/// The strings are passed rather than derived from the data, because nothing in a
/// <see cref="StavkaStatistike"/> knows whether it is a predmet or an oblast. That is
/// deliberate on the row type and it is what makes one partial possible at all; the naming has
/// to come from the screen instead.
/// </para>
/// </remarks>
/// <param name="Naslov">The heading — „Predmeti sa najviše oglasa“.</param>
/// <param name="Opis">The muted line under it, saying what is being counted.</param>
/// <param name="KolonaNaziv">The heading over the naziv column — „Predmet“, „Oblast“.</param>
/// <param name="Prazno">What the table says when there is nothing to rank.</param>
public record RangTabelaViewModel(
    string Naslov,
    string Opis,
    string KolonaNaziv,
    string Prazno,
    IReadOnlyList<StavkaStatistike> Stavke);
