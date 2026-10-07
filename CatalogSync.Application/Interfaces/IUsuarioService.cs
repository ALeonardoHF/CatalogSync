using CatalogSync.Application.DTOs.Auth;
using CatalogSync.Application.DTOs.Usuarios;

namespace CatalogSync.Application.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? rol);
    Task<UsuarioDto> CrearAsync(AdminCrearUsuarioRequest request);
    Task ActivarAsync(Guid id);
    Task DesactivarAsync(Guid id, Guid solicitanteId);
}
