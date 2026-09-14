using booking.core.DTOs;

namespace booking.core.Interfaces;

public interface IAuthService
{
    Task<(bool ok, AuthResponse? data, string? code)> LoginAsync(string email, string password, CancellationToken ct);
    Task<(bool ok, AuthResponse? data, string code, string? message)> RegisterAsync(string email, string password, string? userName, CancellationToken ct);
}
