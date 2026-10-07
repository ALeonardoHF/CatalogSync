namespace LibreriaAtenas.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string NombreCompleto,
    string Email,
    string Role);
