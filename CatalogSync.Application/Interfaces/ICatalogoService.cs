using CatalogSync.Application.Models;
using CatalogSync.Domain.Enums;

namespace CatalogSync.Application.Interfaces;

public interface ICatalogoService
{
    ResumenProceso ProcesarCatalogo(List<LibroExistencia> catalogo, List<LibroProveedor> librosEntrada, EstrategiaPrecio estrategia = EstrategiaPrecio.MasAltoSiHayExistencia);
    string NormalizarISBN(string isbn);
}
