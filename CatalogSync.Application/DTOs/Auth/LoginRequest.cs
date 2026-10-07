using System.ComponentModel.DataAnnotations;

namespace CatalogSync.Application.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
