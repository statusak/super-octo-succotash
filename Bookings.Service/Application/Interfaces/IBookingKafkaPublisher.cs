using CSCourse.Contracts.Kafka;

namespace Bookings.Service.Application.Interfaces;

public interface IBookingKafkaPublisher
{
    Task PublishBookingCreatedAsync(BookingCreated request);
    Task PublishBookingCancellationAsync(BookingCancellation request);
    Task PublishBookingRejectedAsync(BookingRejected request);
    Task PublishBookingCancelledAsync(BookingCancelled request);
}