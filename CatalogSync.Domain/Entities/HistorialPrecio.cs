namespace CatalogSync.Domain.Entities;

public class HistorialPrecio
{
    public Guid Id { get; private set; }
    public Guid LibroId { get; private set; }
    public decimal PrecioAnterior { get; private set; }
    public decimal PrecioNuevo { get; private set; }
    public string Fuente { get; private set; } = string.Empty;
    public Guid? CambiadoPorId { get; private set; }
    public DateTime CambiadoEn { get; private set; }

    public Libro Libro { get; private set; } = null!;

    private HistorialPrecio() { }

    public static HistorialPrecio Create(Guid libroId, decimal anterior, decimal nuevo,
        string fuente, Guid? usuarioId = null) =>
        new()
        {
            Id             = Guid.NewGuid(),
            LibroId        = libroId,
            PrecioAnterior = anterior,
            PrecioNuevo    = nuevo,
            Fuente         = fuente,
            CambiadoPorId  = usuarioId,
            CambiadoEn     = DateTime.UtcNow
        };
}
