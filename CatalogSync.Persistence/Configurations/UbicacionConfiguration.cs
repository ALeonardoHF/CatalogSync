using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class UbicacionConfiguration : IEntityTypeConfiguration<Ubicacion>
{
    public void Configure(EntityTypeBuilder<Ubicacion> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Seccion).HasMaxLength(100);
        b.Property(u => u.Estante).HasMaxLength(50);
        b.Property(u => u.Referencia).HasMaxLength(200);
        b.Property(u => u.Notas).HasMaxLength(500);
    }
}
