namespace LibreriaAtenas.Application.DTOs.Cliente;

public record NotificacionDto(
    Guid Id,
    Guid LibroId,
    string ISBN,
    string Titulo,
    string Autor,
    int? Existencia,
    string Estado,
    DateTime CreadoEn,
    DateTime? EnviadoEn);
