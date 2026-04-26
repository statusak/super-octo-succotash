using CSCourse.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace CSCourse.Services
{
    public class BookingMemoryService : IBookingService
    {
        private readonly ConcurrentDictionary<Guid, Booking> Booking = [];

        public async Task<Booking?> CreateBookingAsync(Guid eventId)
        {
            Guid bookingId;
            do {
                bookingId = Guid.NewGuid();
            } while (Booking.ContainsKey(bookingId));
            
            Booking newBooking = new Booking
            {
                Id = bookingId,
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            if(Booking.TryAdd(bookingId, newBooking))
            {
                return newBooking;
            }

            return null;
        }
        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            if (Booking.TryGetValue(bookingId, out var cached))
                return cached;
            return null;
        }
    }
}
