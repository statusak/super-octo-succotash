using CSCourse.Contracts.Kafka;

namespace Events.Service.Application.Interfaces;

public interface IEventKafkaPublisher
{
    Task PublishBookingResponseAsync(BookingResponse response);
}
