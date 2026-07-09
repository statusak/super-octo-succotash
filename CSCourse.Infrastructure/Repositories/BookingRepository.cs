using CSCourse.Infrastructure.DataAccess;
using CSCourse.Domain.Interfaces;
using CSCourse.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CSCourse.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;
    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Guid Create(BookingRepositoryCreateDto booking)
    {
        var newBooking = new Booking {
            Id = Guid.NewGuid(),
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        _context.Bookings.Add(newBooking);

        try
        {
            _context.SaveChanges();
            return newBooking.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(newBooking).State = EntityState.Detached;
            return Create(booking);
        }
    }
    public async Task<Guid> CreateAsync(BookingRepositoryCreateDto booking)
    {
        var newBooking = new Booking {
            Id = Guid.NewGuid(),
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        _context.Bookings.Add(newBooking);

        try
        {
            await _context.SaveChangesAsync();
            return newBooking.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(newBooking).State = EntityState.Detached;
            return await CreateAsync(booking);
        }
    }

    public IEnumerable<Booking> GetPending()
    {
        return _context.Bookings.Where(x => x.Status == BookingStatus.Pending);
    }

    public async Task<IEnumerable<Booking>> GetPendingAsync()
    {
        return await _context.Bookings.Where(x => x.Status == BookingStatus.Pending).ToListAsync();
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<bool> UpdateAsync(BookingRepositoryUpdateDto booking)
    {
        var rowsAffected = await _context.Bookings
            .Where(x => x.Id == booking.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, b => booking.Status)
                .SetProperty(b => b.ProcessedAt, b => booking.ProcessedAt)
        );

        return rowsAffected > 0;
    }

}