using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).IsRequired();
        b.Property(u => u.NombreCompleto).HasMaxLength(200);
        b.Property(u => u.Role).IsRequired();

        b.HasMany(u => u.RefreshTokens)
         .WithOne(r => r.Usuario)
         .HasForeignKey(r => r.UsuarioId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.Favoritos)
         .WithOne(f => f.Usuario)
         .HasForeignKey(f => f.UsuarioId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
