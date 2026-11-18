namespace Privatni_casovi.Slike;

/// <summary>
/// The profile photo of a korisnik: where it is stored, what an upload has to look like, and
/// the URL a view renders it from.
/// </summary>
/// <remarks>
/// <para>
/// Only the file <i>name</i> is ever stored on <see cref="Models.Korisnik.SlikaFajl"/>. The
/// folder and the default live here, so a view that renders a photo cannot spell the path
/// differently from the action that wrote the file.
/// </para>
/// <para>
/// <c>Slike/</c> is a folder outside the spec's layer list, the same call
/// <c>Identity/</c> made in issue 03: writing a file to <c>wwwroot</c> is neither a controller
/// nor data access, and hiding it in <c>Data/</c> would suggest it touches the database.
/// </para>
/// </remarks>
public static class Fotografija
{
    /// <summary>Rendered for a korisnik who has never uploaded one. Ships with the app.</summary>
    public const string Podrazumevana = "default.png";

    /// <summary>The folder under <c>wwwroot</c>, and the first segment of the public URL.</summary>
    public const string Folder = "slike";

    /// <summary>2 MB, as the issue asks. Well under Kestrel's own 30 MB request limit.</summary>
    public const int NajvecaVelicinaMb = 2;

    /// <summary>The same limit in bytes, which is what an upload is measured in.</summary>
    public const long NajvecaVelicina = NajvecaVelicinaMb * 1024L * 1024L;

    /// <summary>The one list the extension check reads. Everything else below is derived.</summary>
    private static readonly string[] Ekstenzije = { ".jpg", ".jpeg", ".png" };

    /// <summary>
    /// The file input's <c>accept</c> filter, built from <see cref="Ekstenzije"/> so the
    /// picker cannot drift from what the server accepts.
    /// </summary>
    public static readonly string FilterZaIzbor = string.Join(",", Ekstenzije);

    /// <summary>The same list spelled for a person, for the form's own text.</summary>
    /// <remarks>
    /// Deliberately separate from <see cref="FilterZaIzbor"/>: one constant answering to both
    /// a browser and a reader breaks the file picker the first time somebody rewords the
    /// sentence.
    /// </remarks>
    public const string DozvoljeniTipovi = "jpg, jpeg ili png";

    /// <summary>
    /// The URL a view renders. A korisnik with no photo of their own gets
    /// <see cref="Podrazumevana"/> rather than a broken image.
    /// </summary>
    public static string Putanja(string? slikaFajl)
        => $"/{Folder}/{(string.IsNullOrWhiteSpace(slikaFajl) ? Podrazumevana : slikaFajl)}";

    /// <summary>Whether the upload's extension is one this platform accepts.</summary>
    public static bool JeDozvoljenaEkstenzija(string? nazivFajla)
        => Ekstenzije.Contains(Path.GetExtension(nazivFajla ?? string.Empty).ToLowerInvariant());

    /// <summary>
    /// Writes the upload under <c>wwwroot/slike</c> and returns the name to store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name is generated, never taken from the client: two predavači who both upload
    /// <c>slika.jpg</c> must not overwrite each other, and a name the browser supplied is
    /// also the classic way a <c>..\</c> ends up in a path. The only thing kept from the
    /// upload is its extension, and <see cref="JeDozvoljenaEkstenzija"/> has already limited
    /// that to three literals — so nothing a caller sends can decide where this writes.
    /// </para>
    /// <para>
    /// The extension is kept rather than forced to <c>.jpg</c>: static files are served with
    /// the content type their extension implies, and a PNG announced as <c>image/jpeg</c> is
    /// a lie that costs nothing to avoid.
    /// </para>
    /// <para>
    /// Nothing deletes the photo being replaced. Files are not rows, so ADR-0001 has no
    /// opinion here — but the seeded photos are checked into the repository, and deleting one
    /// because its predavač uploaded a new one would leave a fresh clone of a *seeded*
    /// database rendering a broken image.
    /// </para>
    /// </remarks>
    public static async Task<string> SacuvajAsync(IFormFile upload, string korenSajta)
    {
        var naziv = $"{Guid.NewGuid()}{Path.GetExtension(upload.FileName).ToLowerInvariant()}";
        var folder = Path.Combine(korenSajta, Folder);

        Directory.CreateDirectory(folder);

        await using var izlaz = File.Create(Path.Combine(folder, naziv));
        await upload.CopyToAsync(izlaz);

        return naziv;
    }
}
