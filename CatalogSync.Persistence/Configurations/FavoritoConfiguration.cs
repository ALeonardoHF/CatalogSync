using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class FavoritoConfiguration : IEntityTypeConfiguration<Favorito>
{
    public void Configure(EntityTypeBuilder<Favorito> b)
    {
        b.HasKey(f => f.Id);
        b.HasIndex(f => new { f.UsuarioId, f.LibroId }).IsUnique();
    }
}
