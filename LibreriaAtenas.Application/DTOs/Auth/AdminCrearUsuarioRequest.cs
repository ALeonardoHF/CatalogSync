using System.ComponentModel.DataAnnotations;
using LibreriaAtenas.Domain.Enums;

namespace LibreriaAtenas.Application.DTOs.Auth;

public record AdminCrearUsuarioRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string NombreCompleto,
    Role Role = Role.Vendedor);
