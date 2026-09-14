using booking.core.DTOs;

namespace booking.core.Interfaces;

public interface IBookingService
{
    Task<(bool ok, List<BookingModel> data, string code)> GetAllAsync(CancellationToken ct);
    Task<(bool ok, BookingModel? data, string code)> GetByIdAsync(int id, CancellationToken ct);
    Task<(bool ok, List<BookingRow> data, string code)> GetByServiceIdAsync(int serviceId, CancellationToken ct);
    Task<(bool ok, int id, string code, string? message)> CreateAsync(BookingUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> UpdateAsync(int id, BookingUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct);
    Task<(bool ok, List<AvailabilityTimes> data, string? message)> GetAvailability(Availability dto, CancellationToken ct);
}
