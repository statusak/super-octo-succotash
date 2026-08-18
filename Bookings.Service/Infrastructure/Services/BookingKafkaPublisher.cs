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
    private readonly string _topicCancelled; // новый топик

    public BookingKafkaPublisher(
        string bootstrapServers,
        string topicCreated,
        string topicConfirmed,
        string topicRejected,
        string topicCancelled) // добавлен параметр
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            ClientId = "booking-kafka-publisher"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topicCreated = topicCreated;
        _topicConfirmed = topicConfirmed;
        _topicRejected = topicRejected;
        _topicCancelled = topicCancelled;
    }

    public async Task PublishBookingCreatedAsync(BookingCreatedRequest request)
        => await ProduceAsync(_topicCreated, request);

    public async Task PublishBookingConfirmedAsync(CSCourse.Contracts.Models.BookingConfirmed request)
        => await ProduceAsync(_topicConfirmed, request);

    public async Task PublishBookingRejectedAsync(BookingRejectedEvent request)
        => await ProduceAsync(_topicRejected, request);

    public async Task PublishBookingCancelledAsync(BookingCancelledEvent request)
        => await ProduceAsync(_topicCancelled, request);

    private async Task ProduceAsync<T>(string topic, T payload)
    {
        var value = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var key = payload switch
        {
            BookingCreatedRequest bcr => bcr.Id.ToString(),
            BookingConfirmed bcf => bcf.Id.ToString(),
            BookingRejectedEvent bre => bre.Id.ToString(),
            BookingCancelledEvent bce => bce.Id.ToString(), // ключ по Id брони
            _ => Guid.NewGuid().ToString()
        };

        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        await _producer.ProduceAsync(topic, message);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
