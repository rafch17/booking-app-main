using booking.core.DTOs;

namespace booking.core.Interfaces;

public interface IServicesService
{
    Task<(bool ok, List<ServiceModel> data, string code)> GetAllAsync(CancellationToken ct);
    Task<(bool ok, ServiceModel? data, string code)> GetByIdAsync(int id, CancellationToken ct);
    Task<(bool ok, int id, string code, string? message)> CreateAsync(ServiceUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> UpdateAsync(int id, ServiceUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct);
}
