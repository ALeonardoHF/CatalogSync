using CatalogSync.Infrastructure.Services;

namespace CatalogSync.Tests.Services;

public class ArchivoTemporalServiceTests
{
    private static ArchivoTemporalService CreateService() => new();

    [Fact]
    public void Guardar_ThenObtener_ReturnsTheSameBytes()
    {
        var svc = CreateService();
        var datos = new byte[] { 1, 2, 3 };

        var id = svc.Guardar(datos);
        var resultado = svc.Obtener(id);

        Assert.Equal(datos, resultado);
    }

    [Fact]
    public void Obtener_CalledTwice_StillReturnsTheFile()
    {
        // El archivo no debe desaparecer en la primera lectura: un doble
        // clic en "descargar" o un reintento del navegador no debe fallar.
        var svc = CreateService();
        var id = svc.Guardar([9, 8, 7]);

        var primeraLectura = svc.Obtener(id);
        var segundaLectura = svc.Obtener(id);

        Assert.NotNull(primeraLectura);
        Assert.NotNull(segundaLectura);
        Assert.Equal(primeraLectura, segundaLectura);
    }

    [Fact]
    public void Obtener_WithUnknownId_ReturnsNull()
    {
        var svc = CreateService();

        var resultado = svc.Obtener(Guid.NewGuid().ToString());

        Assert.Null(resultado);
    }
}
