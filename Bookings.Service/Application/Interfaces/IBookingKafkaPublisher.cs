namespace Bookings.Service.Application.Interfaces;

public interface IBookingKafkaPublisher
{
    Task PublishBookingCreatedAsync(BookingCreatedRequest request);
    Task PublishBookingConfirmedAsync(CSCourse.Contracts.Models.BookingConfirmed request);
    Task PublishBookingRejectedAsync(BookingRejectedEvent request);
}