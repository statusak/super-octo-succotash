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
            Acks = Acks.All,
            ClientId = "booking-kafka-publisher"
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
            _ => Guid.NewGuid().ToString()
        };

        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        var result = await _producer.ProduceAsync(topic, message);

        // logger.LogInformation("Kafka message sent: Topic={Topic}, Partition={Partition}, Offset={Offset}",
        //     topic, result.Partition, result.Offset);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
