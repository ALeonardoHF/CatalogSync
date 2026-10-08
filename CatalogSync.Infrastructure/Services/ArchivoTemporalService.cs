using System.Collections.Concurrent;
using CatalogSync.Application.Interfaces;

namespace CatalogSync.Infrastructure.Services;

public class ArchivoTemporalService : IArchivoTemporalService
{
    private sealed record Entrada(byte[] Datos, DateTime Expira);

    private readonly ConcurrentDictionary<string, Entrada> _archivos = new();
    private const int TtlMinutos = 30;

    public string Guardar(byte[] contenido)
    {
        PurgarExpirados();
        var id = Guid.NewGuid().ToString();
        _archivos[id] = new Entrada(contenido, DateTime.UtcNow.AddMinutes(TtlMinutos));
        return id;
    }

    public byte[]? Obtener(string id)
    {
        // No se borra al leerlo: si el usuario hace doble clic en
        // "descargar" o el navegador reintenta la petición, el segundo
        // intento debe poder recuperar el mismo archivo. Expira solo por
        // tiempo (TtlMinutos) o cuando PurgarExpirados lo limpia.
        if (!_archivos.TryGetValue(id, out var entrada)) return null;

        if (entrada.Expira < DateTime.UtcNow)
        {
            _archivos.TryRemove(id, out _);
            return null;
        }

        return entrada.Datos;
    }

    private void PurgarExpirados()
    {
        var ahora = DateTime.UtcNow;
        foreach (var (key, entrada) in _archivos)
            if (entrada.Expira < ahora)
                _archivos.TryRemove(key, out _);
    }
}
