using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Infrastructure.Services;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogSync.Tests.Services;

public class NotificacionServiceTests
{
    private static LibreriaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<LibreriaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static NotificacionService CreateService(LibreriaDbContext db) => new(db);

    private static async Task<Libro> SeedLibro(LibreriaDbContext db)
    {
        var libro = Libro.Create("111", "Libro de prueba", "Autor", "Editorial", 100m, 50m);
        db.Libros.Add(libro);
        await db.SaveChangesAsync();
        return libro;
    }

    [Fact]
    public async Task SolicitarAsync_WithValidBook_CreatesSolicitudPendiente()
    {
        using var db = CreateDb();
        var libro = await SeedLibro(db);
        var svc = CreateService(db);
        var usuarioId = Guid.NewGuid();

        var id = await svc.SolicitarAsync(usuarioId, libro.Id);

        var solicitud = await db.NotificacionesSolicitud.FindAsync(id);
        Assert.NotNull(solicitud);
        Assert.Equal(EstadoNotificacion.Pendiente, solicitud!.Estado);
    }

    [Fact]
    public async Task SolicitarAsync_WithNonExistentBook_ThrowsKeyNotFound()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.SolicitarAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task SolicitarAsync_WithExistingPendingRequest_ThrowsInvalidOperation()
    {
        using var db = CreateDb();
        var libro = await SeedLibro(db);
        var svc = CreateService(db);
        var usuarioId = Guid.NewGuid();

        await svc.SolicitarAsync(usuarioId, libro.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.SolicitarAsync(usuarioId, libro.Id));
    }

    [Fact]
    public async Task SolicitarAsync_AfterCancelling_AllowsNewRequest()
    {
        using var db = CreateDb();
        var libro = await SeedLibro(db);
        var svc = CreateService(db);
        var usuarioId = Guid.NewGuid();

        var primeraId = await svc.SolicitarAsync(usuarioId, libro.Id);
        await svc.CancelarAsync(primeraId, usuarioId);

        // No debe lanzar: la anterior ya no está Pendiente.
        var segundaId = await svc.SolicitarAsync(usuarioId, libro.Id);

        Assert.NotEqual(primeraId, segundaId);
    }

    [Fact]
    public async Task CancelarAsync_WithWrongUsuario_ThrowsKeyNotFound()
    {
        using var db = CreateDb();
        var libro = await SeedLibro(db);
        var svc = CreateService(db);
        var id = await svc.SolicitarAsync(Guid.NewGuid(), libro.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.CancelarAsync(id, Guid.NewGuid()));
    }
}
