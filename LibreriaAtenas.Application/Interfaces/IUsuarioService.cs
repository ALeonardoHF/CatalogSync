using LibreriaAtenas.Application.DTOs.Auth;
using LibreriaAtenas.Application.DTOs.Usuarios;

namespace LibreriaAtenas.Application.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? rol);
    Task<UsuarioDto> CrearAsync(AdminCrearUsuarioRequest request);
    Task ActivarAsync(Guid id);
    Task DesactivarAsync(Guid id, Guid solicitanteId);
}
