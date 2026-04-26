using CSCourse.Models;

namespace CSCourse.Services
{
    public interface IBookingService
    {
        Task<Booking?> CreateBookingAsync(Guid eventId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    }
}
