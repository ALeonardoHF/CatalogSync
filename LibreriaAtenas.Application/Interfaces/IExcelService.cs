using LibreriaAtenas.Application.Models;

namespace LibreriaAtenas.Application.Interfaces;

public interface IExcelService
{
    Task<List<LibroExistencia>> LeerExistenciasAsync(Stream stream, string? hoja = null);
    Task<List<LibroProveedor>> LeerProveedorAsync(Stream stream, string nombreProveedor, string? hoja = null);
    Task<byte[]> GenerarExcelActualizadoAsync(List<LibroExistencia> catalogo, Stream streamOriginal, string? hoja = null);
}
