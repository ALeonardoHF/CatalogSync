using CatalogSync.Domain.Enums;
using CatalogSync.Domain.Exceptions;

namespace CatalogSync.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string NombreCompleto { get; private set; } = string.Empty;
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public int TokenVersion { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? ActualizadoEn { get; private set; }
    public DateTime? UltimoLogin { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTime? BloqueadoHasta { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<Favorito> _favoritos = [];
    public IReadOnlyCollection<Favorito> Favoritos => _favoritos.AsReadOnly();

    private Usuario() { }

    public static Usuario Create(string email, string passwordHash, string nombreCompleto, Role role)
    {
        if (string.IsNullOrWhiteSpace(email))     throw new DomainException("Email requerido.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("PasswordHash requerido.");

        return new Usuario
        {
            Id            = Guid.NewGuid(),
            Email         = email.ToLowerInvariant().Trim(),
            PasswordHash  = passwordHash,
            NombreCompleto = nombreCompleto.Trim(),
            Role          = role,
            IsActive      = true,
            TokenVersion  = 1,
            CreadoEn      = DateTime.UtcNow
        };
    }

    public void ActualizarPassword(string nuevoHash)
    {
        PasswordHash = nuevoHash;
        TokenVersion++;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void ActualizarPerfil(string nombreCompleto)
    {
        NombreCompleto = nombreCompleto.Trim();
        ActualizadoEn  = DateTime.UtcNow;
    }

    public void RegistrarLoginExitoso()
    {
        UltimoLogin      = DateTime.UtcNow;
        IntentosFallidos = 0;
        BloqueadoHasta   = null;
    }

    public void RegistrarLoginFallido()
    {
        IntentosFallidos++;
        if (IntentosFallidos >= 5)
            BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
    }

    public bool EstaBloqueado() =>
        BloqueadoHasta.HasValue && BloqueadoHasta.Value > DateTime.UtcNow;

    public void Desactivar() { IsActive = false; ActualizadoEn = DateTime.UtcNow; }
    public void Activar()    { IsActive = true;  ActualizadoEn = DateTime.UtcNow; }
    public void IncrementarTokenVersion() { TokenVersion++; }
}
