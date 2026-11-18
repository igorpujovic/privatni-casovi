using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Models;

namespace Privatni_casovi.Data;

public class ApplicationDbContext : IdentityDbContext<Korisnik>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Oblast> Oblasti => Set<Oblast>();

    public DbSet<Predmet> Predmeti => Set<Predmet>();

    public DbSet<Oglas> Oglasi => Set<Oglas>();

    public DbSet<Ocena> Ocene => Set<Ocena>();

    public DbSet<Omiljeni> Omiljeni => Set<Omiljeni>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Korisnik>(korisnik =>
        {
            korisnik.Property(k => k.Ime).IsRequired().HasMaxLength(100);
            korisnik.Property(k => k.Prezime).IsRequired().HasMaxLength(100);
            korisnik.Property(k => k.Telefon).IsRequired().HasMaxLength(30);
            korisnik.Property(k => k.Biografija).HasMaxLength(1000);
            korisnik.Property(k => k.SlikaFajl).HasMaxLength(255);
        });

        builder.Entity<Oblast>(oblast =>
        {
            oblast.Property(o => o.Naziv).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Predmet>(predmet =>
        {
            predmet.Property(p => p.Naziv).IsRequired().HasMaxLength(100);

            predmet.HasOne(p => p.Oblast)
                .WithMany(o => o.Predmeti)
                .HasForeignKey(p => p.OblastId)
                .IsRequired();
        });

        // The relationships into Korisnik below all use a bare WithMany() on purpose. A
        // predavač must never see who rated them or who favourited them (spec rule 3), so
        // Korisnik deliberately has no Ocene or Omiljeni collection to navigate.
        builder.Entity<Oglas>(oglas =>
        {
            oglas.Property(o => o.CenaCasa).HasColumnType("decimal(10,2)");
            oglas.Property(o => o.Dostupnost).IsRequired().HasMaxLength(500);

            oglas.HasOne(o => o.Predavac)
                .WithMany()
                .HasForeignKey(o => o.PredavacId)
                .IsRequired();

            oglas.HasOne(o => o.Predmet)
                .WithMany(p => p.Oglasi)
                .HasForeignKey(o => o.PredmetId)
                .IsRequired();
        });

        builder.Entity<Ocena>(ocena =>
        {
            // One ocena per (učenik, predavač) pair — the učenik edits that row rather than
            // adding a second one.
            ocena.HasIndex(o => new { o.UcenikId, o.PredavacId }).IsUnique();

            // The spec's "Vrednost (1–5)" at the schema level. The user-facing [Range(1,5)]
            // on the ViewModel is issue 15's — without it an out-of-range POST surfaces as a
            // DbUpdateException rather than a ModelState error.
            ocena.ToTable(t => t.HasCheckConstraint(
                "CK_Ocena_Vrednost", "[Vrednost] BETWEEN 1 AND 5"));

            ocena.HasOne(o => o.Ucenik)
                .WithMany()
                .HasForeignKey(o => o.UcenikId)
                .IsRequired();

            ocena.HasOne(o => o.Predavac)
                .WithMany()
                .HasForeignKey(o => o.PredavacId)
                .IsRequired();
        });

        builder.Entity<Omiljeni>(omiljeni =>
        {
            omiljeni.HasKey(o => new { o.UcenikId, o.PredavacId });

            omiljeni.HasOne(o => o.Ucenik)
                .WithMany()
                .HasForeignKey(o => o.UcenikId)
                .IsRequired();

            omiljeni.HasOne(o => o.Predavac)
                .WithMany()
                .HasForeignKey(o => o.PredavacId)
                .IsRequired();
        });

        // Nothing on this platform is ever deleted — korisnici and oglasi are deactivated
        // with an Aktivan flag — so no relationship may cascade. ADR-0001 asks for a rule
        // that "can't be forgotten": stating it once here covers the Identity relationships
        // too (AspNetUserRoles and friends default to Cascade, and they are FKs to Korisnik
        // like any other), and it covers every relationship a later issue adds.
        //
        // Must stay last: it rewrites the relationships configured above.
        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
