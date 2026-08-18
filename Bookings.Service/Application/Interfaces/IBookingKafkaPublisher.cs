using CSCourse.Contracts.Models;

namespace Bookings.Service.Application.Interfaces;

public interface IBookingKafkaPublisher
{
    Task PublishBookingCreatedAsync(BookingCreatedRequest request);
    Task PublishBookingConfirmedAsync(BookingConfirmed request);
    Task PublishBookingRejectedAsync(BookingRejectedEvent request);
}