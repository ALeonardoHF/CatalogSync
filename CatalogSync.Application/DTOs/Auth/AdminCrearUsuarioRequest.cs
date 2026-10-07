using System.ComponentModel.DataAnnotations;
using CatalogSync.Domain.Enums;

namespace CatalogSync.Application.DTOs.Auth;

public record AdminCrearUsuarioRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string NombreCompleto,
    Role Role = Role.Vendedor);
