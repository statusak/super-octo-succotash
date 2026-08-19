namespace Bookings.Service.Application.Interfaces;

public interface IBookingKafkaConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
    Task StopConsumingAsync();
}
