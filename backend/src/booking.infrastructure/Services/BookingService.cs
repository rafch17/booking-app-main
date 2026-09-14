using System.Globalization;
using Microsoft.EntityFrameworkCore;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Interfaces;
using booking.core.Models;
using booking.infrastructure.Persistence;

namespace booking.infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    public BookingService(AppDbContext db) => _db = db;

    public async Task<(bool ok, List<BookingModel> data, string code)> GetAllAsync(CancellationToken ct)
    {
        var items = await _db.Bookings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => MapToModel(s))
            .ToListAsync(ct);

        return (true, items, Codes.Ok);
    }

    public async Task<(bool ok, BookingModel? data, string code)> GetByIdAsync(int id, CancellationToken ct)
    {
        var s = await _db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null) return (false, null, Codes.NotFound);
        return (true, MapToModel(s), Codes.Ok);
    }

    public async Task<(bool ok, List<BookingRow> data, string code)> GetByServiceIdAsync(
    int serviceId,
    CancellationToken ct)
    {
        var items = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.ServiceDetail!.ServiceId == serviceId)
            .OrderBy(b => b.Id)
            .Select(b => new BookingRow
            {
                Id = b.Id,
                EmployeeName = b.Employee != null
                    ? (b.Employee.FirstName + " " + b.Employee.LastName)
                    : string.Empty,
                ServiceDetailName = b.ServiceDetail != null
                    ? b.ServiceDetail.Name
                    : string.Empty,

                StartDateTime = b.StartDatetime,
                EndDateTime = b.EndDatetime
            })
            .ToListAsync(ct);

        return (true, items, Codes.Ok);
    }


    public async Task<(bool ok, int id, string code, string? message)> CreateAsync(BookingUpsertDto dto, CancellationToken ct)
    {
        var s = new Booking
        {
            ServiceDetailId = dto.serviceDetailId,
            EmployeeId = dto.employeeId,
            StartDatetime = dto.startDatetime,
            EndDatetime = dto.endDatetime,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(s);
        await _db.SaveChangesAsync(ct);
        return (true, s.Id, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> UpdateAsync(int id, BookingUpsertDto dto, CancellationToken ct)
    {
        var s = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Booking not found.");

        s.ServiceDetailId = dto.serviceDetailId;
        s.EmployeeId = dto.employeeId;
        s.StartDatetime = dto.startDatetime;
        s.EndDatetime = dto.endDatetime;

        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct)
    {
        var s = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Booking not found.");

        _db.Bookings.Remove(s);
        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    public async Task<(bool ok, List<AvailabilityTimes> data, string? message)> GetAvailability(Availability dto, CancellationToken ct)
    {
        if (!DateTime.TryParse(dto.date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return (false, new List<AvailabilityTimes>(), "Invalid date format.");

        if (!int.TryParse(dto.frequency, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frequencyMinutes) || frequencyMinutes <= 0)
            return (false, new List<AvailabilityTimes>(), "Invalid frequency; it must be a positive number of minutes.");

        var initialHour = TimeSpan.Parse("08:00", CultureInfo.InvariantCulture);
        var finalHour = TimeSpan.Parse("15:00", CultureInfo.InvariantCulture);
        var availabilityList = new List<AvailabilityTimes>();

        var date = parsedDate.Date;
        DateTime startTime = date.Add(initialHour);
        DateTime endTime = date.Add(finalHour);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var bookingsOnDate = await _db.Bookings
            .AsNoTracking()
            .Where(x =>
                x.ServiceDetailId == dto.id &&
                x.StartDatetime >= dayStart &&
                x.StartDatetime < dayEnd
            )
            .ToListAsync(ct);

        for (var time = startTime; time < endTime; time = time.AddMinutes(frequencyMinutes))
        {
            var isAvailable = true;

            foreach (var b in bookingsOnDate)
            {
                if (b.StartDatetime <= time && b.EndDatetime > time)
                {
                    isAvailable = false;
                    break;
                }
            }

            availabilityList.Add(new AvailabilityTimes(time.ToString("HH:mm"), isAvailable));
        }

        return (true, availabilityList, null);
    }

    private static BookingModel MapToModel(Booking s) =>
        new(
            s.Id,
            s.ServiceDetailId,
            s.EmployeeId,
            s.StartDatetime,
            s.EndDatetime,
            s.CreatedAt
        );
}