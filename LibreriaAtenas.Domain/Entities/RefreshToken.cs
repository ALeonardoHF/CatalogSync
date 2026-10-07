namespace LibreriaAtenas.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiraEn { get; private set; }
    public bool Revocado { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public string? DireccionIp { get; private set; }

    public Usuario Usuario { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid usuarioId, string token, int diasExpiracion, string? ip = null) =>
        new()
        {
            Id          = Guid.NewGuid(),
            UsuarioId   = usuarioId,
            Token       = token,
            ExpiraEn    = DateTime.UtcNow.AddDays(diasExpiracion),
            CreadoEn    = DateTime.UtcNow,
            DireccionIp = ip
        };

    public bool EsValido() => !Revocado && ExpiraEn > DateTime.UtcNow;
    public void Revocar()  { Revocado = true; }
}
