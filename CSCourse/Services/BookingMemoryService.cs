using CSCourse.Interfaces;
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
        public async Task<Booking?> CreateBookingAsync(Guid eventId)
        {
            Guid bookingId;
            Booking newBooking;
            bool canReserveSeats;
            lock (_bookingLock)
            {
                canReserveSeats = _eventService.TryReserveSeats(eventId);
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
            }

            if(!canReserveSeats)
            {
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            return null;
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
