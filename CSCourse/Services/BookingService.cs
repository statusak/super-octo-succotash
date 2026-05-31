using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Middlewares;
using CSCourse.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CSCourse.Services
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException(string message) : base(message) { }
    }

    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;

        private readonly AppDbContext _context;
        private readonly SemaphoreSlim _processingSemaphoreBooking = new(1, 1);
        private readonly object _bookingLock = new();

        public BookingService(
            IEventService eventService, AppDbContext context)
        {
            _eventService = eventService;
            _context = context;
        }
        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            Guid bookingId;
            Booking newBooking;
            bool canReserveSeats;
            // TODO: Здесь было-бы уместно использовать транзакцию
            await _processingSemaphoreBooking.WaitAsync();
            try
            {
                try
                {
                    canReserveSeats = await _eventService.TryReserveSeatsAsync(eventId);
                }
                catch (InvalidOperationException) 
                {
                    throw new NotFoundException($"not found event with id {eventId}");
                }
                catch
                {
                    throw;
                }
                if (canReserveSeats)
                {

                    bookingId = Guid.NewGuid();
                    newBooking = new Booking
                    {
                        Id = bookingId,
                        EventId = eventId,
                        Status = BookingStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Bookings.AddAsync(newBooking);
                    await _context.SaveChangesAsync();

                    return newBooking;
                }

                    throw new NoAvailableSeatsException("No available seats for this event");
            }
            finally
            {
                _processingSemaphoreBooking.Release();
            }
        }

        public IEnumerable<Booking> GetPending()
        {
            var pendingBooking = _context.Bookings.Where(x => x.Status == BookingStatus.Pending);
            return pendingBooking;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        }
        public async Task<Booking?> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking)
        {
            var cached = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (cached != null)
            {
                cached.Status = booking.Status;
                cached.ProcessedAt = booking.ProcessedAt;

                await _context.SaveChangesAsync();

                return cached;
            }
            return null;
        }
    }
}
