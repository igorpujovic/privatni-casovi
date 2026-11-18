using Microsoft.AspNetCore.Identity;

namespace Privatni_casovi.Models;

/// <summary>
/// A registered account. Exactly one of predavač, učenik, or administrator —
/// the role is an Identity role, fixed at registration.
/// </summary>
public class Korisnik : IdentityUser
{
    public string Ime { get; set; } = string.Empty;

    public string Prezime { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;

    public string? Biografija { get; set; }

    /// <summary>File name of the profile photo under wwwroot, if one was uploaded.</summary>
    public string? SlikaFajl { get; set; }

    /// <summary>Nothing is deleted — a blocked korisnik is deactivated. See ADR-0001.</summary>
    public bool Aktivan { get; set; } = true;
}
