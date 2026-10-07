using LibreriaAtenas.Domain.Enums;

namespace LibreriaAtenas.Domain.Entities;

public class Inventario
{
    public Guid Id { get; private set; }
    public Guid LibroId { get; private set; }
    public int Existencia { get; private set; }
    public int Ventas { get; private set; }
    public EstadoInventario Estado { get; private set; }
    public DateTime? FechaEntrada { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    public Libro Libro { get; private set; } = null!;

    private Inventario() { }

    public static Inventario Create(Guid libroId, int existencia, DateTime? fechaEntrada = null) =>
        new()
        {
            Id           = Guid.NewGuid(),
            LibroId      = libroId,
            Existencia   = existencia,
            Estado       = existencia > 0 ? EstadoInventario.Disponible : EstadoInventario.Agotado,
            FechaEntrada = fechaEntrada,
            ActualizadoEn = DateTime.UtcNow
        };

    public void ActualizarExistencia(int nuevaExistencia)
    {
        Existencia    = nuevaExistencia;
        Estado        = nuevaExistencia > 0 ? EstadoInventario.Disponible : EstadoInventario.Agotado;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void RegistrarVenta(int cantidad = 1)
    {
        Existencia    = Math.Max(0, Existencia - cantidad);
        Ventas        += cantidad;
        Estado        = Existencia > 0 ? EstadoInventario.Disponible : EstadoInventario.Agotado;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void CambiarEstado(EstadoInventario nuevoEstado)
    {
        Estado        = nuevoEstado;
        ActualizadoEn = DateTime.UtcNow;
    }
}
