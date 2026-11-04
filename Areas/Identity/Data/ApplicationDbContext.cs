using aluguel.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace aluguel.Areas.Identity.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {

        base.OnModelCreating(builder);
        builder.HasDefaultSchema("rentall");
        // Connect evaluations to their explicit foreign-key properties.
        builder.Entity<Avaliacao>()
            .HasOne(a => a.item).WithMany()
            .HasForeignKey(a => a.iditem).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Avaliacao>()
            .HasOne(a => a.anuncio).WithMany()
            .HasForeignKey(a => a.idanuncio).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PedidoAluguer>().HasIndex(p => new { p.AnuncianteId, p.CriadoEm });
        builder.Entity<Mensagem>().HasIndex(m => new { m.DestinatarioId, m.AnuncioId, m.CriadaEm });
        builder.Entity<Favorito>()
            .HasIndex(f => new { f.UserId, f.AnuncioId }).IsUnique();
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ApplicationUserEntityConfiguration());
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Favorito> Favoritos { get; set; } = null!;
    public DbSet<Mensagem> Mensagens { get; set; } = null!;
    public DbSet<PedidoAluguer> PedidosAluguer { get; set; } = null!;
    public DbSet<Anuncio> Anuncios { get; set; }
    public DbSet<Avaliacao> Avaliacoes { get; set; }
    public DbSet<ImagensItem> ImagensItems { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Proposta> Propostas { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Tipo> Tipos { get; set; }
}

public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(255);
        builder.Property(u => u.LastName).HasMaxLength(255);
        builder.Property(u => u.CreatedAt);
    }
}