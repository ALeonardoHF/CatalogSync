namespace LibreriaAtenas.Application.DTOs.Usuarios;

public record UsuarioDto(
    Guid Id,
    string Email,
    string NombreCompleto,
    string Role,
    bool IsActive,
    DateTime CreadoEn,
    DateTime? UltimoLogin);
