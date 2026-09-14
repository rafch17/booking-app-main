using booking.core.DTOs;

namespace booking.core.Interfaces;

public interface IOfficeService
{
    Task<(bool ok, List<OfficeModel> data, string code)> GetAllAsync(CancellationToken ct);
    Task<(bool ok, OfficeModel? data, string code)> GetByIdAsync(int id, CancellationToken ct);
    Task<(bool ok, int id, string code, string? message)> CreateAsync(OfficeUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> UpdateAsync(int id, OfficeUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct);
}
