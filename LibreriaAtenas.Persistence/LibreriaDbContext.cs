using LibreriaAtenas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LibreriaAtenas.Persistence;

public class LibreriaDbContext : DbContext
{
    public LibreriaDbContext(DbContextOptions<LibreriaDbContext> options) : base(options) { }

    public DbSet<Usuario>              Usuarios              => Set<Usuario>();
    public DbSet<RefreshToken>         RefreshTokens         => Set<RefreshToken>();
    public DbSet<Libro>                Libros                => Set<Libro>();
    public DbSet<Inventario>           Inventarios           => Set<Inventario>();
    public DbSet<Ubicacion>            Ubicaciones           => Set<Ubicacion>();
    public DbSet<HistorialPrecio>      HistorialPrecios      => Set<HistorialPrecio>();
    public DbSet<Favorito>             Favoritos             => Set<Favorito>();
    public DbSet<NotificacionSolicitud> NotificacionesSolicitud => Set<NotificacionSolicitud>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
