using CSCourse.Interfaces;
using CSCourse.Middlewares;
using CSCourse.Models;
using System.Collections.Concurrent;

namespace CSCourse.Services
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException(string message) : base(message) { }
    }

    public class BookingMemoryService : IBookingService
    {
        private readonly IEventService _eventService;

        private readonly ConcurrentDictionary<Guid, Booking> Booking = [];
        private readonly object _bookingLock = new();

        public BookingMemoryService(
            IEventService eventService)
        {
            _eventService = eventService;
        }
        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            Guid bookingId;
            Booking newBooking;
            bool canReserveSeats;
            lock (_bookingLock)
            {
                try
                {
                    canReserveSeats = _eventService.TryReserveSeats(eventId);
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
                    do
                    {
                        bookingId = Guid.NewGuid();
                        newBooking = new Booking
                        {
                            Id = bookingId,
                            EventId = eventId,
                            Status = BookingStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                        };
                    } while (!Booking.TryAdd(bookingId, newBooking));
                    
                    return newBooking;
                }

                throw new NoAvailableSeatsException("No available seats for this event");
            }
        }

        public IEnumerable<Booking> GetPending()
        {
            var allBooking = Booking.Values;
            var pendingBooking = allBooking.Where(x => x.Status == BookingStatus.Pending);
            return pendingBooking;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            if (Booking.TryGetValue(bookingId, out var cached))
                return cached;
            return null;
        }
        public async Task<Booking?> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking)
        {
            if (Booking.TryGetValue(bookingId, out var cached))
            {
                cached.Status = booking.Status;
                cached.ProcessedAt = booking.ProcessedAt;

                return cached;
            }
            return null;
        }
    }
}
