namespace LibreriaAtenas.Application.Interfaces;

public interface IArchivoTemporalService
{
    string Guardar(byte[] contenido);
    byte[]? Obtener(string id);
}
