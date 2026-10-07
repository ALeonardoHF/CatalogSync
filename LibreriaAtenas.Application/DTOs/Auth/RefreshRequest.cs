using System.ComponentModel.DataAnnotations;

namespace LibreriaAtenas.Application.DTOs.Auth;

public record RefreshRequest([Required] string RefreshToken);
