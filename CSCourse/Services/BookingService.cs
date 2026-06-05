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

        private readonly IBookingRepository _bookings;

        private readonly SemaphoreSlim _processingSemaphoreBooking = new(1, 1);
        private readonly object _bookingLock = new();

        public BookingService(
            IEventService eventService, IBookingRepository bookings)
        {
            _eventService = eventService;
            _bookings = bookings;
        }
        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
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
                    var newBookingDto = new BookingRepositoryCreateDto
                    {
                        EventId = eventId,
                        Status = BookingStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    Guid bookingId = await _bookings.CreateAsync(newBookingDto);

                    var newBooking = new Booking
                    {
                        Id = bookingId, 
                        EventId = newBookingDto.EventId,
                        Status = newBookingDto.Status,
                        CreatedAt = newBookingDto.CreatedAt,
                    };

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
            var pendingBooking = _bookings.GetPending();
            return pendingBooking;
        }

        public async Task<IEnumerable<Booking>> GetPendingAsync()
        {
            var pendingBooking = await _bookings.GetPendingAsync();
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
