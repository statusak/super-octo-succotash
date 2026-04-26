using CSCourse.Models;

namespace CSCourse.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
        Task<Booking?> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking);
    }
}
