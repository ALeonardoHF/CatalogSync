using LibreriaAtenas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibreriaAtenas.Persistence.Configurations;

public class HistorialPrecioConfiguration : IEntityTypeConfiguration<HistorialPrecio>
{
    public void Configure(EntityTypeBuilder<HistorialPrecio> b)
    {
        b.HasKey(h => h.Id);
        b.Property(h => h.PrecioAnterior).HasColumnType("decimal(10,2)");
        b.Property(h => h.PrecioNuevo).HasColumnType("decimal(10,2)");
        b.Property(h => h.Fuente).HasMaxLength(100);
    }
}
