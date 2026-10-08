using CatalogSync.Application.Interfaces;
using CatalogSync.Application.Models;
using CatalogSync.Domain.Enums;
using CatalogSync.Domain.Servicios;

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

        resumen.IsbnInvalidos = librosEntrada.Count(l => string.IsNullOrWhiteSpace(l.ISBN));

        // Si el mismo ISBN viene de mas de un proveedor en el mismo lote
        // (ej. OCEANO y PLANETA cotizan el mismo libro), se toma el que
        // ofrece el precio mas alto — sin importar el orden en que se
        // subieron los archivos. Antes ganaba "el ultimo procesado",
        // que dependia del orden de los entradas en el formulario.
        var porIsbn = librosEntrada
            .Where(l => !string.IsNullOrWhiteSpace(l.ISBN))
            .GroupBy(l => l.ISBN)
            .Select(g => g.OrderByDescending(l => l.PrecioUnitario).First());

        foreach (var libro in porIsbn)
        {
            if (catalogoDict.TryGetValue(libro.ISBN, out var existente))
            {
                var existenciaActual = int.TryParse(existente.Existencia, out var e) ? e : 0;
                var precioResuelto = estrategia.ResolverPrecio(existente.Precio, libro.PrecioUnitario, existenciaActual);

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
                RevisionDeCampos.Revisar(diferencias, "Título", existente.Titulo, libro.Nombre, valor => existente.Titulo = valor);
                RevisionDeCampos.Revisar(diferencias, "Editorial", existente.Editorial, libro.Editorial, valor => existente.Editorial = valor);
                RevisionDeCampos.Revisar(diferencias, "Sello", existente.Sello, libro.Sello, valor => existente.Sello = valor);

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

    public string NormalizarISBN(string isbn) => isbn.Trim().Trim('*').Trim();
}
