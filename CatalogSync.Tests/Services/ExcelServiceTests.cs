using CatalogSync.Application.Models;
using CatalogSync.Infrastructure.Services;
using OfficeOpenXml;

namespace CatalogSync.Tests.Services;

public class ExcelServiceTests
{
    static ExcelServiceTests() => ExcelPackage.License.SetNonCommercialPersonal("CatalogSync.Tests");

    private static readonly string[] Encabezados =
        ["ISBN", "TITULO", "AUTOR", "DesCuento", "EDITORIAL", "COSTO", "inc.", "PRECIO", "FENTRADA", "CodigoBArrAs", "EXISTENCIA", "VENTAS"];

    private static ExcelService CreateService() => new();

    private static byte[] BuildWorkbook(params object[][] filas)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Existencias");
        ws.Cells[1, 1].LoadFromArrays(filas);
        return package.GetAsByteArray();
    }

    // ── LeerExistenciasAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task LeerExistenciasAsync_ParsesRowAndSplitsEditorialSello()
    {
        var bytes = BuildWorkbook(
            Encabezados,
            ["*9781234567897*", "El libro de prueba", "Autor Uno", "10", "OCEANO (SELLO ESPECIAL)", "100", "1.16", 250.0, "2024-01-01", "7501234567897", "5", "2"]);

        var svc = CreateService();
        using var stream = new MemoryStream(bytes);
        var resultado = await svc.LeerExistenciasAsync(stream);

        Assert.Equal(Encabezados, resultado.Encabezados);
        Assert.Single(resultado.Libros);

        var libro = resultado.Libros[0];
        Assert.Equal("9781234567897", libro.ISBN);
        Assert.Equal("*9781234567897*", libro.ISBNOriginal);
        Assert.Equal("El libro de prueba", libro.Titulo);
        Assert.Equal("OCEANO", libro.Editorial);
        Assert.Equal("SELLO ESPECIAL", libro.Sello);
        Assert.Equal(250m, libro.Precio);
        Assert.Equal("1.16", libro.Inc);
    }

    [Fact]
    public async Task LeerExistenciasAsync_SkipsRowsWithBlankIsbn()
    {
        var bytes = BuildWorkbook(
            Encabezados,
            ["", "Sin ISBN", "Autor", "0", "OCEANO", "0", "0", 0.0, "", "", "0", "0"],
            ["111", "Con ISBN", "Autor", "0", "OCEANO", "0", "0", 50.0, "", "", "0", "0"]);

        var svc = CreateService();
        using var stream = new MemoryStream(bytes);
        var resultado = await svc.LeerExistenciasAsync(stream);

        Assert.Single(resultado.Libros);
        Assert.Equal("111", resultado.Libros[0].ISBN);
    }

    // ── GenerarExcelActualizadoAsync ──────────────────────────────────────────

    [Fact]
    public async Task GenerarExcelActualizadoAsync_RoundTripsExistingRowAfterPriceUpdate()
    {
        var catalogo = new List<LibroExistencia>
        {
            new()
            {
                ISBN = "111", ISBNOriginal = "111", Titulo = "Libro A", Autor = "Autor A",
                Descuento = "10", Editorial = "OCEANO", Sello = "SELLO", Costo = "100",
                Inc = "1.16", Precio = 350m, FechaEntrada = "2024-01-01", CodigoBarra = "7501111111111",
                Existencia = "5", Ventas = "2", FilaOriginal = 2
            }
        };

        var svc = CreateService();
        var bytes = await svc.GenerarExcelActualizadoAsync(catalogo, Encabezados);

        using var stream = new MemoryStream(bytes);
        var releido = await svc.LeerExistenciasAsync(stream);

        Assert.Equal(Encabezados, releido.Encabezados);
        Assert.Single(releido.Libros);

        var libro = releido.Libros[0];
        Assert.Equal("111", libro.ISBN);
        Assert.Equal("Libro A", libro.Titulo);
        Assert.Equal("OCEANO", libro.Editorial);
        Assert.Equal("SELLO", libro.Sello);
        Assert.Equal(350m, libro.Precio);
        Assert.Equal("1.16", libro.Inc);
    }

    [Fact]
    public async Task GenerarExcelActualizadoAsync_AppendsNewBooksAfterExisting()
    {
        var catalogo = new List<LibroExistencia>
        {
            new() { ISBN = "111", ISBNOriginal = "111", Titulo = "Existente", Editorial = "OCEANO", Precio = 100m, FilaOriginal = 2 },
            new() { ISBN = "222", ISBNOriginal = "222", Titulo = "Nuevo", Editorial = "PLANETA", Sello = "INFANTIL", Precio = 59m, FilaOriginal = 0 }
        };

        var svc = CreateService();
        var bytes = await svc.GenerarExcelActualizadoAsync(catalogo, Encabezados);

        using var stream = new MemoryStream(bytes);
        var releido = await svc.LeerExistenciasAsync(stream);

        Assert.Equal(2, releido.Libros.Count);

        var nuevo = releido.Libros.Single(l => l.ISBN == "222");
        Assert.Equal("PLANETA", nuevo.Editorial);
        Assert.Equal("INFANTIL", nuevo.Sello);
        Assert.Equal(3, nuevo.FilaOriginal); // fila 1 = encabezado, 2 = existente, 3 = nuevo
    }

    [Fact]
    public async Task GenerarExcelActualizadoAsync_WritesNumericFieldsAsNumbersNotText()
    {
        var catalogo = new List<LibroExistencia>
        {
            new()
            {
                ISBN = "111", ISBNOriginal = "111", Titulo = "Libro", Editorial = "OCEANO",
                Costo = "100", Existencia = "5", Ventas = "2", Precio = 250m, FilaOriginal = 2
            }
        };

        var svc = CreateService();
        var bytes = await svc.GenerarExcelActualizadoAsync(catalogo, Encabezados);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];

        Assert.IsType<double>(ws.Cells[2, 6].Value);  // COSTO
        Assert.IsType<double>(ws.Cells[2, 11].Value); // EXISTENCIA
        Assert.IsType<double>(ws.Cells[2, 12].Value); // VENTAS
    }

    [Fact]
    public async Task GenerarExcelActualizadoAsync_WithNoExistingRows_OnlyWritesNewBooks()
    {
        var catalogo = new List<LibroExistencia>
        {
            new() { ISBN = "999", ISBNOriginal = "999", Titulo = "Unico nuevo", Editorial = "OCEANO", Precio = 10m, FilaOriginal = 0 }
        };

        var svc = CreateService();
        var bytes = await svc.GenerarExcelActualizadoAsync(catalogo, Encabezados);

        using var stream = new MemoryStream(bytes);
        var releido = await svc.LeerExistenciasAsync(stream);

        Assert.Single(releido.Libros);
        Assert.Equal("999", releido.Libros[0].ISBN);
    }
}
