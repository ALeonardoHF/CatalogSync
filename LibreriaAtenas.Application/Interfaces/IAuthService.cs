using LibreriaAtenas.Application.DTOs.Auth;

namespace LibreriaAtenas.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress = null);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllAsync(Guid usuarioId);
}
