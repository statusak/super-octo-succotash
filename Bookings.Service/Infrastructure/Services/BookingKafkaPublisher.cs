using Confluent.Kafka;
using Bookings.Service.Application.Interfaces;
using CSCourse.Contracts.Models;
using System.Text.Json;

namespace Bookings.Service.Infrastructure.Services;

public class BookingKafkaPublisher : IBookingKafkaPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topicCreated;
    private readonly string _topicConfirmed;
    private readonly string _topicRejected;

    public BookingKafkaPublisher(
        string bootstrapServers,
        string topicCreated,
        string topicConfirmed,
        string topicRejected)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topicCreated = topicCreated;
        _topicConfirmed = topicConfirmed;
        _topicRejected = topicRejected;
    }

    public async Task PublishBookingCreatedAsync(BookingCreatedRequest request)
        => await ProduceAsync(_topicCreated, request);

    public async Task PublishBookingConfirmedAsync(BookingConfirmed request)
        => await ProduceAsync(_topicConfirmed, request);

    public async Task PublishBookingRejectedAsync(BookingRejectedEvent request)
        => await ProduceAsync(_topicRejected, request);

    private async Task ProduceAsync<T>(string topic, T payload)
    {
        var value = JsonSerializer.Serialize(payload);
        var key = payload switch
        {
            BookingCreatedRequest bcr => bcr.Id.ToString(),
            CSCourse.Contracts.Models.BookingConfirmed bcf => bcf.Id.ToString(),
            BookingRejectedEvent bre => bre.BookingId.ToString(),
            _ => Guid.NewGuid().ToString()
        };

        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        // Key важен: все брони на одно событие попадут в один партишн — это защитит от гонки за места
        var result = await _producer.ProduceAsync(topic, message);
        // В продакшене тут можно логировать Partition/Offset для трассировки
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
