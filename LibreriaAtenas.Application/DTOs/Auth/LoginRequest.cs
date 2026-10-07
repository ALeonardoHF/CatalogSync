using System.ComponentModel.DataAnnotations;

namespace LibreriaAtenas.Application.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
