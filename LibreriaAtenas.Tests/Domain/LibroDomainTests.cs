using LibreriaAtenas.Domain.Entities;
using LibreriaAtenas.Domain.Exceptions;

namespace LibreriaAtenas.Tests.Domain;

public class LibroDomainTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var libro = Libro.Create("978-0-06-112008-4", "Cien años de soledad", "García Márquez", "Sudamericana", 350m);

        Assert.Equal("978-0-06-112008-4", libro.ISBN);
        Assert.Equal("Cien años de soledad", libro.Titulo);
        Assert.Equal("García Márquez", libro.Autor);
        Assert.Equal(350m, libro.PrecioVenta);
        Assert.True(libro.IsActive);
        Assert.NotEqual(Guid.Empty, libro.Id);
    }

    [Fact]
    public void Create_WithZeroPrecio_Succeeds()
    {
        var libro = Libro.Create("978-123", "Titulo", "Autor", "Editorial", 0m);

        Assert.Equal(0m, libro.PrecioVenta);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyISBN_ThrowsDomainException(string isbn)
    {
        Assert.Throws<DomainException>(() =>
            Libro.Create(isbn, "Titulo", "Autor", "Editorial", 100m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitulo_ThrowsDomainException(string titulo)
    {
        Assert.Throws<DomainException>(() =>
            Libro.Create("978-123", titulo, "Autor", "Editorial", 100m));
    }

    [Fact]
    public void Create_WithNegativePrecio_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Libro.Create("978-123", "Titulo", "Autor", "Editorial", -1m));
    }

    // ── ActualizarPrecio ──────────────────────────────────────────────────────

    [Fact]
    public void ActualizarPrecio_WithValidPrice_UpdatesPrice()
    {
        var libro = CrearLibro();

        libro.ActualizarPrecio(500m);

        Assert.Equal(500m, libro.PrecioVenta);
        Assert.NotNull(libro.ActualizadoEn);
    }

    [Fact]
    public void ActualizarPrecio_WithNegativePrice_ThrowsDomainException()
    {
        var libro = CrearLibro();

        Assert.Throws<DomainException>(() => libro.ActualizarPrecio(-10m));
    }

    [Fact]
    public void ActualizarPrecio_WithZero_Succeeds()
    {
        var libro = CrearLibro();

        libro.ActualizarPrecio(0m);

        Assert.Equal(0m, libro.PrecioVenta);
    }

    // ── Activar / Desactivar ──────────────────────────────────────────────────

    [Fact]
    public void Desactivar_SetsIsActiveToFalse()
    {
        var libro = CrearLibro();

        libro.Desactivar();

        Assert.False(libro.IsActive);
        Assert.NotNull(libro.ActualizadoEn);
    }

    [Fact]
    public void Activar_SetsIsActiveToTrue()
    {
        var libro = CrearLibro();
        libro.Desactivar();

        libro.Activar();

        Assert.True(libro.IsActive);
    }

    // ── ActualizarMetadatos ───────────────────────────────────────────────────

    [Fact]
    public void ActualizarMetadatos_UpdatesFields()
    {
        var libro = CrearLibro();

        libro.ActualizarMetadatos("Nuevo Titulo", "Nuevo Autor", "Nueva Editorial", 80m, 10m);

        Assert.Equal("Nuevo Titulo", libro.Titulo);
        Assert.Equal("Nuevo Autor", libro.Autor);
        Assert.Equal("Nueva Editorial", libro.Editorial);
        Assert.Equal(80m, libro.Costo);
        Assert.Equal(10m, libro.Descuento);
        Assert.NotNull(libro.ActualizadoEn);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Libro CrearLibro() =>
        Libro.Create("978-000-123", "Titulo de prueba", "Autor", "Editorial", 200m);
}
