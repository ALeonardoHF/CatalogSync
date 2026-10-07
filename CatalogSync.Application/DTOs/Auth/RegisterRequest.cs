using System.ComponentModel.DataAnnotations;

namespace CatalogSync.Application.DTOs.Auth;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(100)] string NombreCompleto);
