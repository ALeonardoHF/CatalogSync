using CatalogSync.Application.Interfaces;
using CatalogSync.Application.Models;
using CatalogSync.Domain.Enums;

namespace CatalogSync.Infrastructure.Services;

public class CatalogoService : ICatalogoService
{
    public ResumenProceso ProcesarCatalogo(List<LibroExistencia> catalogo, List<LibroProveedor> librosEntrada, EstrategiaPrecio estrategia = EstrategiaPrecio.MasAltoSiHayExistencia)
    {
        var resumen = new ResumenProceso { TotalCatalogo = catalogo.Count };

        var catalogoDict = catalogo
            .Where(l => !string.IsNullOrWhiteSpace(l.ISBN))
            .GroupBy(l => l.ISBN)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var libro in librosEntrada)
        {
            if (string.IsNullOrWhiteSpace(libro.ISBN)) { resumen.IsbnInvalidos++; continue; }

            if (catalogoDict.TryGetValue(libro.ISBN, out var existente))
            {
                var existenciaActual = int.TryParse(existente.Existencia, out var e) ? e : 0;
                var precioResuelto = ResolverPrecio(estrategia, existente.Precio, libro.PrecioUnitario, existenciaActual);

                if (precioResuelto != existente.Precio)
                {
                    resumen.Cambios.Add(new DetalleCambio
                    {
                        ISBN = libro.ISBN,
                        Titulo = existente.Titulo,
                        Proveedor = libro.Proveedor,
                        PrecioAnterior = existente.Precio,
                        PrecioNuevo = precioResuelto,
                        Resultado = "Actualizado"
                    });
                    existente.Precio = precioResuelto;
                    resumen.Actualizados++;
                }
                else resumen.SinCambio++;

                var diferencias = new List<string>();
                RevisarCampo(diferencias, "Título", existente.Titulo, libro.Nombre, valor => existente.Titulo = valor);
                RevisarCampo(diferencias, "Editorial", existente.Editorial, libro.Editorial, valor => existente.Editorial = valor);
                RevisarCampo(diferencias, "Sello", existente.Sello, libro.Sello, valor => existente.Sello = valor);

                if (diferencias.Count > 0)
                {
                    resumen.Revisar++;
                    resumen.Cambios.Add(new DetalleCambio
                    {
                        ISBN = libro.ISBN,
                        Titulo = existente.Titulo,
                        Proveedor = libro.Proveedor,
                        PrecioAnterior = existente.Precio,
                        PrecioNuevo = existente.Precio,
                        Resultado = "Revisar",
                        Detalle = string.Join("; ", diferencias)
                    });
                }
            }
            else
            {
                var nuevo = new LibroExistencia
                {
                    ISBN = libro.ISBN,
                    ISBNOriginal = libro.ISBN,
                    Titulo = libro.Nombre,
                    Autor = libro.Autor,
                    Editorial = libro.Editorial,
                    Sello = libro.Sello,
                    Precio = libro.PrecioUnitario,
                    FilaOriginal = 0
                };
                catalogo.Add(nuevo);
                catalogoDict[libro.ISBN] = nuevo;

                resumen.Nuevos++;
                resumen.Cambios.Add(new DetalleCambio
                {
                    ISBN = libro.ISBN,
                    Titulo = libro.Nombre,
                    Proveedor = libro.Proveedor,
                    PrecioAnterior = 0,
                    PrecioNuevo = libro.PrecioUnitario,
                    Resultado = "Nuevo"
                });
            }
        }

        return resumen;
    }

    private static decimal ResolverPrecio(EstrategiaPrecio estrategia, decimal precioActual, decimal precioNuevo, int existenciaActual)
    {
        return estrategia switch
        {
            EstrategiaPrecio.SiempreElMasAlto => Math.Max(precioActual, precioNuevo),
            EstrategiaPrecio.SiempreElNuevo => precioNuevo,
            EstrategiaPrecio.MasAltoSiHayExistencia => existenciaActual > 0
                ? Math.Max(precioActual, precioNuevo)
                : precioNuevo,
            _ => precioNuevo
        };
    }

    private static void RevisarCampo(List<string> diferencias, string campo, string actual, string nuevo, Action<string> completar)
    {
        if (string.IsNullOrWhiteSpace(nuevo)) return;

        if (string.IsNullOrWhiteSpace(actual))
        {
            completar(nuevo);
            return;
        }

        if (!string.Equals(actual, nuevo, StringComparison.OrdinalIgnoreCase))
            diferencias.Add($"{campo}: '{actual}' ≠ '{nuevo}'");
    }

    public string NormalizarISBN(string isbn) => isbn.Trim().Trim('*').Trim();
}
