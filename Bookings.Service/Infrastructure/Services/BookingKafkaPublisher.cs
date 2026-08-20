using System.Text.Json;
using Bookings.Service.Application.Interfaces;
using Confluent.Kafka;
using CSCourse.Contracts.Kafka;
using Microsoft.Extensions.Logging;

namespace Bookings.Service.Infrastructure.Services;

public class BookingKafkaPublisher : IBookingKafkaPublisher
{
    private readonly ProducerConfig _producerConfig;
    private readonly ILogger<BookingKafkaPublisher> _logger;

    public BookingKafkaPublisher(
        ILogger<BookingKafkaPublisher> logger,
        string bootstrapServers)
    {
        _logger = logger;

        _producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        };
    }

    public async Task PublishBookingCreatedAsync(BookingCreated request)
    {
        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            var result = await producer.ProduceAsync(KafkaTopics.BookingCreated, new Message<string, string>
            {
                Key = request.Id.ToString(),
                Value = JsonSerializer.Serialize(request)
            });
            _logger.LogInformation(
                "BookingCreated published to topic '{Topic}' with offset {Offset} on partition {Partition}",
                                                    result.Topic, result.Offset, result.Partition);
        } catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "BookingCreated failed to publish to topic '{Topic}': {Error}",
                                  KafkaTopics.BookingCreated, ex.Error.Reason);

            throw;
        }
    }

    public async Task PublishBookingCancellationAsync(BookingCancellation request)
    {
        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            var result = await producer.ProduceAsync(KafkaTopics.BookingCancellation, new Message<string, string>
            {
                Key = request.Id.ToString(),
                Value = JsonSerializer.Serialize(request)
            });
            _logger.LogInformation(
                "BookingCancellation published to topic '{Topic}' with offset {Offset} on partition {Partition}",
                                                    result.Topic, result.Offset, result.Partition);
        } catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "BookingCancellation failed to publish to topic '{Topic}': {Error}",
                                  KafkaTopics.BookingCancellation, ex.Error.Reason);

            throw;
        }
    }
}
