using CatalogSync.Application.DTOs.Libros;
using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Infrastructure.Services;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogSync.Tests.Services;

public class LibroServiceTests
{
    private static LibreriaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<LibreriaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static LibroService CreateService(LibreriaDbContext db) => new(db);

    private static CrearLibroRequest BuildRequest(string isbn = "978-0-306-40615-7") =>
        new(isbn, "El libro de prueba", "Autor Prueba", "Editorial Prueba", 250m, 100m);

    // ── CrearAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_WithValidData_ReturnsLibroDto()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal("978-0-306-40615-7", result.ISBN);
        Assert.Equal("El libro de prueba", result.Titulo);
        Assert.True(result.IsActive);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CrearAsync_WithDuplicateISBN_ThrowsInvalidOperation()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CrearAsync(BuildRequest(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CrearAsync_CreatesInventarioWithZeroExistencia()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        Assert.Equal(0, result.Existencia);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsLibroDto()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var creado = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        var result = await svc.GetByIdAsync(creado.Id);

        Assert.NotNull(result);
        Assert.Equal(creado.Id, result.Id);
        Assert.Equal(creado.ISBN, result.ISBN);
    }

    // ── GetByIsbnAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIsbnAsync_WithNonExistentISBN_ReturnsNull()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.GetByIsbnAsync("000-000-000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIsbnAsync_WithExistingISBN_ReturnsLibroDto()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        await svc.CrearAsync(BuildRequest("978-111"), Guid.NewGuid());

        var result = await svc.GetByIsbnAsync("978-111");

        Assert.NotNull(result);
        Assert.Equal("978-111", result.ISBN);
    }

    // ── DesactivarAsync / ActivarAsync ────────────────────────────────────────

    [Fact]
    public async Task DesactivarAsync_WithNonExistentId_ThrowsKeyNotFound()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.DesactivarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DesactivarAsync_SetsIsActiveToFalse()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        await svc.DesactivarAsync(libro.Id);
        var result = await svc.GetByIdAsync(libro.Id);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task ActivarAsync_WithNonExistentId_ThrowsKeyNotFound()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ActivarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ActivarAsync_SetsIsActiveToTrue()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());
        await svc.DesactivarAsync(libro.Id);

        await svc.ActivarAsync(libro.Id);
        var result = await svc.GetByIdAsync(libro.Id);

        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    // ── ActualizarInventarioAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ActualizarInventarioAsync_WithNonExistentId_ThrowsKeyNotFound()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ActualizarInventarioAsync(Guid.NewGuid(), new ActualizarInventarioRequest(10)));
    }

    [Fact]
    public async Task ActualizarInventarioAsync_UpdatesExistencia()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        await svc.ActualizarInventarioAsync(libro.Id, new ActualizarInventarioRequest(25));
        var result = await svc.GetByIdAsync(libro.Id);

        Assert.NotNull(result);
        Assert.Equal(25, result.Existencia);
    }

    [Fact]
    public async Task ActualizarInventarioAsync_FromZeroToPositive_ResolvesPendingNotifications()
    {
        // "Avisarme cuando haya existencia" no servia de nada si nadie
        // resolvia la solicitud cuando el libro se reabastecia.
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());

        var solicitud = NotificacionSolicitud.Create(Guid.NewGuid(), libro.Id);
        db.NotificacionesSolicitud.Add(solicitud);
        await db.SaveChangesAsync();

        await svc.ActualizarInventarioAsync(libro.Id, new ActualizarInventarioRequest(5));

        var actualizada = await db.NotificacionesSolicitud.FindAsync(solicitud.Id);
        Assert.Equal(EstadoNotificacion.Enviada, actualizada!.Estado);
    }

    [Fact]
    public async Task ActualizarInventarioAsync_DoesNotResolveNotificationsForOtherBooks()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libroRestockeado = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        var otroLibro = await svc.CrearAsync(BuildRequest("222"), Guid.NewGuid());

        var solicitudOtroLibro = NotificacionSolicitud.Create(Guid.NewGuid(), otroLibro.Id);
        db.NotificacionesSolicitud.Add(solicitudOtroLibro);
        await db.SaveChangesAsync();

        await svc.ActualizarInventarioAsync(libroRestockeado.Id, new ActualizarInventarioRequest(5));

        var sinTocar = await db.NotificacionesSolicitud.FindAsync(solicitudOtroLibro.Id);
        Assert.Equal(EstadoNotificacion.Pendiente, sinTocar!.Estado);
    }

    // ── BuscarAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuscarAsync_WithNoBooks_ReturnsEmptyPage()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.BuscarAsync(null, 1, 10);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task BuscarAsync_WithMatchingSearchTerm_ReturnsFiltered()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        await svc.CrearAsync(
            new CrearLibroRequest("222", "Java Programming", "Author", "Editorial", 100m, 50m),
            Guid.NewGuid());

        // Mismas mayusculas que el titulo real: el proveedor InMemory de
        // EF Core compara strings distinguiendo mayusculas, a diferencia
        // de SQL Server con su collation por default (case-insensitive).
        // La busqueda insensible a mayusculas es una garantia de SQL
        // Server, no de la consulta LINQ — este test no puede verificarla
        // con InMemory.
        var result = await svc.BuscarAsync("Java", 1, 10);

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("Java Programming", result.Items[0].Titulo);
    }

    [Fact]
    public async Task BuscarAsync_FiltersByIsActive()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest(), Guid.NewGuid());
        await svc.DesactivarAsync(libro.Id);

        var activos = await svc.BuscarAsync(null, 1, 10, isActive: true);
        var inactivos = await svc.BuscarAsync(null, 1, 10, isActive: false);

        Assert.Equal(0, activos.Total);
        Assert.Equal(1, inactivos.Total);
    }

    [Fact]
    public async Task BuscarAsync_WithSoloConExistencia_ExcludesZeroStock()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro1 = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        var libro2 = await svc.CrearAsync(BuildRequest("222"), Guid.NewGuid());
        await svc.ActualizarInventarioAsync(libro1.Id, new ActualizarInventarioRequest(5));

        var result = await svc.BuscarAsync(null, 1, 10, soloConExistencia: true);

        Assert.Equal(1, result.Total);
        Assert.Equal("111", result.Items[0].ISBN);
        _ = libro2;
    }

    // ── DesactivarAgotadosAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DesactivarAgotadosAsync_DeactivatesOnlyZeroStockBooks()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var agotado = await svc.CrearAsync(BuildRequest("000"), Guid.NewGuid());
        var conExistencia = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        await svc.ActualizarInventarioAsync(conExistencia.Id, new ActualizarInventarioRequest(3));

        var result = await svc.DesactivarAgotadosAsync();

        Assert.Equal(1, result.Afectados);
        var agotadoDb = await svc.GetByIdAsync(agotado.Id);
        var activoDb = await svc.GetByIdAsync(conExistencia.Id);
        Assert.False(agotadoDb!.IsActive);
        Assert.True(activoDb!.IsActive);
    }

    // ── ImportarCatalogoAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ImportarCatalogoAsync_WithStock_KeepsHigherPriceEvenIfItemIsLower()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        await svc.ActualizarInventarioAsync(libro.Id, new ActualizarInventarioRequest(5));

        var items = new List<LibroImportItem>
        {
            new("111", "El libro de prueba", "Autor Prueba", "Editorial Prueba", 100m, 50m, null, 5, 0, false)
        };

        var result = await svc.ImportarCatalogoAsync(items, Guid.NewGuid());

        var actualizado = await svc.GetByIdAsync(libro.Id);
        Assert.Equal(250m, actualizado!.PrecioVenta); // se queda el mayor, 250 > 100
        Assert.Equal(1, result.SinCambio);
        Assert.Equal(0, result.PreciosActualizados);
    }

    [Fact]
    public async Task ImportarCatalogoAsync_WithoutStock_AlwaysTakesItemPriceEvenIfLower()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        // Existencia queda en 0 por default al crear.

        var items = new List<LibroImportItem>
        {
            new("111", "El libro de prueba", "Autor Prueba", "Editorial Prueba", 100m, 50m, null, 0, 0, false)
        };

        var result = await svc.ImportarCatalogoAsync(items, Guid.NewGuid());

        var actualizado = await svc.GetByIdAsync(libro.Id);
        Assert.Equal(100m, actualizado!.PrecioVenta); // sin existencia, siempre toma el nuevo
        Assert.Equal(1, result.PreciosActualizados);
    }

    [Fact]
    public async Task ImportarCatalogoAsync_CompletesEmptyEditorial_WithoutOverwritingExistingTitle()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(
            new CrearLibroRequest("111", "Mi libro", "Autor Original", "", 250m, 100m),
            Guid.NewGuid());
        await svc.ActualizarInventarioAsync(libro.Id, new ActualizarInventarioRequest(5));

        var items = new List<LibroImportItem>
        {
            new("111", "Otro título", "Autor Original", "Editorial Nueva", 250m, 100m, null, 5, 0, false)
        };

        var result = await svc.ImportarCatalogoAsync(items, Guid.NewGuid());

        var actualizado = await svc.GetByIdAsync(libro.Id);
        Assert.Equal("Mi libro", actualizado!.Titulo);       // no se sobrescribe
        Assert.Equal("Editorial Nueva", actualizado.Editorial); // estaba vacía, se completa
        Assert.Equal(1, result.Revisar);
        Assert.Contains("Título:", result.MensajesRevisar![0]);
    }

    [Fact]
    public async Task ImportarCatalogoAsync_WhenBookRestocksFromZero_ResolvesPendingNotifications()
    {
        using var db = CreateDb();
        var svc = CreateService(db);
        var libro = await svc.CrearAsync(BuildRequest("111"), Guid.NewGuid());
        // Existencia queda en 0 por default al crear.

        var solicitud = NotificacionSolicitud.Create(Guid.NewGuid(), libro.Id);
        db.NotificacionesSolicitud.Add(solicitud);
        await db.SaveChangesAsync();

        var items = new List<LibroImportItem>
        {
            new("111", "El libro de prueba", "Autor Prueba", "Editorial Prueba", 250m, 100m, null, 5, 0, false)
        };

        await svc.ImportarCatalogoAsync(items, Guid.NewGuid());

        var actualizada = await db.NotificacionesSolicitud.FindAsync(solicitud.Id);
        Assert.Equal(EstadoNotificacion.Enviada, actualizada!.Estado);
    }

    [Fact]
    public async Task ImportarCatalogoAsync_CreatesNewBookWhenIsbnNotFound()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var items = new List<LibroImportItem>
        {
            new("999", "Libro nuevo", "Autor", "Editorial", 80m, 40m, null, 3, 0, true)
        };

        var result = await svc.ImportarCatalogoAsync(items, Guid.NewGuid());

        Assert.Equal(1, result.Creados);
        var creado = await svc.GetByIsbnAsync("999");
        Assert.NotNull(creado);
        Assert.Equal(80m, creado!.PrecioVenta);
    }
}
