namespace CatalogSync.Domain.Enums;

public static class EstrategiaPrecioExtensions
{
    /// <summary>
    /// Decide el precio resultante segun la estrategia configurada. Logica
    /// unica, compartida por el motor de reconciliacion de Excel
    /// (CatalogoService) y la importacion directa a la base de datos
    /// (LibroService), para que procesar, importar-bd e importar-archivo
    /// se comporten igual en vez de tener cada uno su propia regla.
    /// </summary>
    public static decimal ResolverPrecio(this EstrategiaPrecio estrategia, decimal precioActual, decimal precioNuevo, int existenciaActual) =>
        estrategia switch
        {
            EstrategiaPrecio.SiempreElMasAlto => Math.Max(precioActual, precioNuevo),
            EstrategiaPrecio.SiempreElNuevo => precioNuevo,
            EstrategiaPrecio.MasAltoSiHayExistencia => existenciaActual > 0
                ? Math.Max(precioActual, precioNuevo)
                : precioNuevo,
            _ => precioNuevo
        };
}
