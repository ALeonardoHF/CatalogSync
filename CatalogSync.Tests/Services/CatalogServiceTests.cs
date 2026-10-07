using CatalogSync.Application.Models;
using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Infrastructure.Services;

namespace CatalogSync.Tests.Services;

public class CatalogServiceTests
{
    private static CatalogoService CreateService() => new();

    private static LibroExistencia BuildLibro(decimal precio, string existencia, string titulo = "Libro de prueba", string editorial = "", string sello = "") => new()
    {
        ISBN = "111",
        ISBNOriginal = "111",
        Titulo = titulo,
        Editorial = editorial,
        Sello = sello,
        Precio = precio,
        Existencia = existencia
    };

    private static LibroProveedor BuildProveedor(decimal precio, string nombre = "Libro de prueba", string editorial = "", string sello = "") => new()
    {
        ISBN = "111",
        Nombre = nombre,
        Editorial = editorial,
        Sello = sello,
        PrecioUnitario = precio,
        Proveedor = "OCEANO"
    };

    // SiempreElMasAlto

    [Fact]
    public void SiempreElMasAlto_KeepsExistingWhenNewIsLower()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "0") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)], EstrategiaPrecio.SiempreElMasAlto);

        Assert.Equal(300m, catalogo[0].Precio);
        Assert.Equal(1, resumen.SinCambio);
    }

    [Fact]
    public void SiempreElMasAlto_TakesNewWhenHigher()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "0") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(350m)], EstrategiaPrecio.SiempreElMasAlto);

        Assert.Equal(350m, catalogo[0].Precio);
        Assert.Equal(1, resumen.Actualizados);
    }

    // SiempreElNuevo
    [Fact]
    public void SiempreElNuevo_TakesNewEvenWhenLower()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "5") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)], EstrategiaPrecio.SiempreElNuevo);

        Assert.Equal(250m, catalogo[0].Precio);
        Assert.Equal(1, resumen.Actualizados);
    }

    //MasAltoSiHayExistencia
    [Fact]
    public void MalAltoSiHayExistencia_KeepsHigherWhenStockExists()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "5") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)], EstrategiaPrecio.MasAltoSiHayExistencia);

        Assert.Equal(300m, catalogo[0].Precio);
        Assert.Equal(1, resumen.SinCambio);
    }

    [Fact]
    public void MasAltoSiHayExistencia_TakesNewWhenNoStock()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "0") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)], EstrategiaPrecio.MasAltoSiHayExistencia);

        Assert.Equal(250m, catalogo[0].Precio);
        Assert.Equal(1, resumen.Actualizados);
    }

    [Fact]
    public void MasAltoSiHayExistencia_TreatsUnparseableStockAsZero()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "") };

        CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)], EstrategiaPrecio.MasAltoSiHayExistencia);

        Assert.Equal(250m, catalogo[0].Precio);
    }

    // Libros nuevos
    [Fact]
    public void ProcesarCatalogo_AddsUnknownIsbnAsNuevo()
    {
        var catalogo = new List<LibroExistencia>();
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m)]);

        Assert.Single(catalogo);
        Assert.Equal(1, resumen.Nuevos);
        Assert.Equal("Nuevo", resumen.Cambios[0].Resultado);
    }

    // Revisar cambios titulo vacio en el catalogo
    [Fact]
    public void ProcesarCatalogo_TituloVacioEnCatalogo()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "5", titulo: "") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, nombre: "Libro nuevo")]);

        Assert.Equal("Libro nuevo", catalogo[0].Titulo);
        Assert.Empty(resumen.Cambios);
    }

    [Fact]
    public void ProcesarCatalogo_TituloDistinto()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "2", titulo: "Titulo 1") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, nombre: "Titulo 2")]);

        Assert.Equal(1, resumen.Revisar);
        Assert.Equal("Titulo 1", catalogo[0].Titulo);
        Assert.Contains("Título:", resumen.Cambios[0].Detalle);
    }

    [Fact]
    public void ProcesarCatalogo_EditorialDistinta()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "1", editorial: "Editorial 1") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, editorial: "Editorial 2")]);

        Assert.Equal(1, resumen.Revisar);
        Assert.Contains("Editorial:", resumen.Cambios[0].Detalle);
    }

    [Fact]
    public void ProcesarCatalogo_SelloVacioEnCatalogo()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "1", sello: "") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, sello: "Sello 2")]);

        Assert.Equal(0, resumen.Revisar);
        Assert.Equal("Sello 2", catalogo[0].Sello);
    }

    [Fact]
    public void ProcesarCatalogo_SelloDistinto()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "2", sello: "Sello 1") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, sello: "Sello 2")]);

        Assert.Equal(1, resumen.Revisar);
        Assert.Equal("Sello 1", catalogo[0].Sello);
        Assert.Contains("Sello:", resumen.Cambios[0].Detalle);
    }

    [Fact]
    public void ProcesarCatalogo_MismoTituloConMayusculasDistintas()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "2", titulo: "Titulo Uno") };
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(300m, nombre: "Titulo uno")]);

        Assert.Equal(0, resumen.Revisar);
        Assert.Equal("Titulo Uno", catalogo[0].Titulo);
    }

    [Fact]
    public void ProcesarCatalogo_PrecioSubeYTituloDistintoAlMismoTiempo()
    {
        var catalogo = new List<LibroExistencia> { BuildLibro(300m, "2", titulo: "Titulo 1") };
        var resumen  = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(350m, nombre: "Titulo 2")]);

        Assert.Equal(1, resumen.Revisar);
        Assert.Equal(1, resumen.Actualizados);
        Assert.Equal(2, resumen.Cambios.Count);
        Assert.Equal("Actualizado", resumen.Cambios[0].Resultado);
        Assert.Equal("Revisar", resumen.Cambios[1].Resultado);
    }

    [Fact]
    public void ProcesarCatalogo_LibroNuevo()
    {
        var catalogo = new List<LibroExistencia>();
        var resumen = CreateService().ProcesarCatalogo(catalogo, [BuildProveedor(250m, sello: "Sello 2")]);

        Assert.Equal(1, resumen.Nuevos);
        Assert.Equal(0, resumen.Actualizados);
        Assert.Equal("Sello 2", catalogo[0].Sello);
    }
}