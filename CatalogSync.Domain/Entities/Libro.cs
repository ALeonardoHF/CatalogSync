using CatalogSync.Domain.Exceptions;

namespace CatalogSync.Domain.Entities;

public class Libro
{
    public Guid Id { get; private set; }
    public string ISBN { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string Autor { get; private set; } = string.Empty;
    public string Editorial { get; private set; } = string.Empty;
    public decimal PrecioVenta { get; private set; }
    public decimal Costo { get; private set; }
    public decimal Descuento { get; private set; }
    public string? Portada { get; private set; }
    public string? Sinopsis { get; private set; }
    public string? Genero { get; private set; }
    public int? Paginas { get; private set; }
    public int? AnioPublicacion { get; private set; }
    public string? CodigoBarra { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? ActualizadoEn { get; private set; }
    public bool IsActive { get; private set; }

    public Inventario? Inventario { get; private set; }
    public Ubicacion? Ubicacion { get; private set; }

    private readonly List<HistorialPrecio> _historialPrecios = [];
    public IReadOnlyCollection<HistorialPrecio> HistorialPrecios => _historialPrecios.AsReadOnly();

    private readonly List<Favorito> _favoritos = [];
    public IReadOnlyCollection<Favorito> Favoritos => _favoritos.AsReadOnly();

    private readonly List<NotificacionSolicitud> _notificaciones = [];
    public IReadOnlyCollection<NotificacionSolicitud> Notificaciones => _notificaciones.AsReadOnly();

    private Libro() { }

    public static Libro Create(string isbn, string titulo, string autor, string editorial, decimal precioVenta, decimal costo = 0)
    {
        if (string.IsNullOrWhiteSpace(isbn))   throw new DomainException("ISBN requerido.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título requerido.");
        if (precioVenta < 0) throw new DomainException("El precio no puede ser negativo.");

        return new Libro
        {
            Id          = Guid.NewGuid(),
            ISBN        = isbn.Trim(),
            Titulo      = titulo.Trim(),
            Autor       = autor.Trim(),
            Editorial   = editorial.Trim(),
            PrecioVenta = precioVenta,
            Costo       = costo,
            IsActive    = true,
            CreadoEn    = DateTime.UtcNow
        };
    }

    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio < 0) throw new DomainException("El precio no puede ser negativo.");
        PrecioVenta   = nuevoPrecio;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void ActualizarMetadatos(string titulo, string autor, string editorial, decimal costo, decimal descuento)
    {
        Titulo        = titulo.Trim();
        Autor         = autor.Trim();
        Editorial     = editorial.Trim();
        Costo         = costo;
        Descuento     = descuento;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void ActualizarDatosAdicionales(string? portada, string? sinopsis, string? genero,
        int? paginas, int? anio, string? codigoBarra)
    {
        Portada         = portada;
        Sinopsis        = sinopsis;
        Genero          = genero;
        Paginas         = paginas;
        AnioPublicacion = anio;
        CodigoBarra     = codigoBarra?.Trim();
        ActualizadoEn   = DateTime.UtcNow;
    }

    public void ActualizarPortada(string? portada)
    {
        Portada = portada;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void Desactivar() { IsActive = false; ActualizadoEn = DateTime.UtcNow; }
    public void Activar()    { IsActive = true;  ActualizadoEn = DateTime.UtcNow; }
}
