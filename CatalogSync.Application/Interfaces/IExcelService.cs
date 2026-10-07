using CatalogSync.Application.Models;

namespace CatalogSync.Application.Interfaces;

public interface IExcelService
{
    Task<CatalogoLeido> LeerExistenciasAsync(Stream stream, string? hoja = null);
    Task<List<LibroProveedor>> LeerProveedorAsync(Stream stream, string nombreProveedor, string? hoja = null);
    Task<byte[]> GenerarExcelActualizadoAsync(List<LibroExistencia> catalogo, string[] encabezados);
}
