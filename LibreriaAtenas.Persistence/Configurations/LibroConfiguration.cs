using LibreriaAtenas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibreriaAtenas.Persistence.Configurations;

public class LibroConfiguration : IEntityTypeConfiguration<Libro>
{
    public void Configure(EntityTypeBuilder<Libro> b)
    {
        b.HasKey(l => l.Id);
        b.Property(l => l.ISBN).IsRequired().HasMaxLength(30);
        b.HasIndex(l => l.ISBN).IsUnique();
        b.Property(l => l.Titulo).IsRequired().HasMaxLength(500);
        b.Property(l => l.Autor).HasMaxLength(300);
        b.Property(l => l.Editorial).HasMaxLength(200);
        b.Property(l => l.PrecioVenta).HasColumnType("decimal(10,2)");
        b.Property(l => l.Costo).HasColumnType("decimal(10,2)");
        b.Property(l => l.Descuento).HasColumnType("decimal(5,2)");
        b.Property(l => l.Portada).HasMaxLength(1000);
        b.Property(l => l.Genero).HasMaxLength(100);
        b.Property(l => l.CodigoBarra).HasMaxLength(50);

        b.HasOne(l => l.Inventario)
         .WithOne(i => i.Libro)
         .HasForeignKey<Inventario>(i => i.LibroId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(l => l.Ubicacion)
         .WithOne(u => u.Libro)
         .HasForeignKey<Ubicacion>(u => u.LibroId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(l => l.HistorialPrecios)
         .WithOne(h => h.Libro)
         .HasForeignKey(h => h.LibroId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(l => l.Favoritos)
         .WithOne(f => f.Libro)
         .HasForeignKey(f => f.LibroId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(l => l.Notificaciones)
         .WithOne(n => n.Libro)
         .HasForeignKey(n => n.LibroId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
