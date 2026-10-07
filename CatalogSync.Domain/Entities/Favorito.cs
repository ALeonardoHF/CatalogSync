namespace CatalogSync.Domain.Entities;

public class Favorito
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid LibroId { get; private set; }
    public DateTime CreadoEn { get; private set; }

    public Usuario Usuario { get; private set; } = null!;
    public Libro Libro { get; private set; } = null!;

    private Favorito() { }

    public static Favorito Create(Guid usuarioId, Guid libroId) =>
        new()
        {
            Id        = Guid.NewGuid(),
            UsuarioId = usuarioId,
            LibroId   = libroId,
            CreadoEn  = DateTime.UtcNow
        };
}
