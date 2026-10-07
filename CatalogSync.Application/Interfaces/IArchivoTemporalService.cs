namespace CatalogSync.Application.Interfaces;

public interface IArchivoTemporalService
{
    string Guardar(byte[] contenido);
    byte[]? Obtener(string id);
}
