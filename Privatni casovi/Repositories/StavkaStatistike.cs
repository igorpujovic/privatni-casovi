namespace Privatni_casovi.Repositories;

/// <summary>
/// One row of a „sa najviše oglasa“ ranking — a predmet or an oblast, and how many visible
/// oglasi it carries.
/// </summary>
/// <remarks>
/// <para>
/// One record for both rankings, because they are the same question asked one level apart and
/// the statistika screen renders them with the same two columns. A predmet record and an
/// oblast record would differ in nothing but their own names.
/// </para>
/// <para>
/// <b>It counts oglasi, which is all it can honestly count.</b> An ocena attaches to the
/// predavač and never to the oglas, so how sought-after a predmet is cannot be computed from
/// anything this platform stores — a predmet with many oglasi is a predmet many predavači
/// offer, not one many učenici want. That is why the heading over these tables reads „sa
/// najviše oglasa“ and not „najtraženiji“, and why this field is named after what it counted.
/// </para>
/// </remarks>
public record StavkaStatistike(string Naziv, int BrojOglasa);
