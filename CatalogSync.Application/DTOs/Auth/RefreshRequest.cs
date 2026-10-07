using System.ComponentModel.DataAnnotations;

namespace CatalogSync.Application.DTOs.Auth;

public record RefreshRequest([Required] string RefreshToken);
