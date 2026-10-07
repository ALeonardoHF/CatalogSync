using CatalogSync.Domain.Enums;

namespace CatalogSync.Domain.Entities;

public class Ubicacion
{
    public Guid Id { get; private set; }
    public Guid LibroId { get; private set; }
    public TipoUbicacion Tipo { get; private set; }
    public string? Seccion { get; private set; }
    public string? Estante { get; private set; }
    public string? Referencia { get; private set; }
    public string? Notas { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    public Libro Libro { get; private set; } = null!;

    private Ubicacion() { }

    public static Ubicacion Create(Guid libroId, TipoUbicacion tipo, string? seccion = null,
        string? estante = null, string? referencia = null, string? notas = null) =>
        new()
        {
            Id            = Guid.NewGuid(),
            LibroId       = libroId,
            Tipo          = tipo,
            Seccion       = seccion?.Trim(),
            Estante       = estante?.Trim(),
            Referencia    = referencia?.Trim(),
            Notas         = notas?.Trim(),
            ActualizadoEn = DateTime.UtcNow
        };

    public void Actualizar(TipoUbicacion tipo, string? seccion, string? estante, string? referencia, string? notas)
    {
        Tipo          = tipo;
        Seccion       = seccion?.Trim();
        Estante       = estante?.Trim();
        Referencia    = referencia?.Trim();
        Notas         = notas?.Trim();
        ActualizadoEn = DateTime.UtcNow;
    }
}
