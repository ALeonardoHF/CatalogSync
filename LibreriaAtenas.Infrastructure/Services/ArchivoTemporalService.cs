using System.Collections.Concurrent;
using LibreriaAtenas.Application.Interfaces;

namespace LibreriaAtenas.Infrastructure.Services;

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
        if (!_archivos.TryGetValue(id, out var entrada)) return null;
        _archivos.TryRemove(id, out _);
        return entrada.Expira >= DateTime.UtcNow ? entrada.Datos : null;
    }

    private void PurgarExpirados()
    {
        var ahora = DateTime.UtcNow;
        foreach (var (key, entrada) in _archivos)
            if (entrada.Expira < ahora)
                _archivos.TryRemove(key, out _);
    }
}
