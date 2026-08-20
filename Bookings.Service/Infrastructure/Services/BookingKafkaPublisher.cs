using System.Text.Json;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Infrastructure.Config;
using Confluent.Kafka;
using CSCourse.Contracts.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookings.Service.Infrastructure.Services;

public class BookingKafkaPublisher : IBookingKafkaPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<BookingKafkaPublisher> _logger;
    private bool _disposed;

    public BookingKafkaPublisher(
        ILogger<BookingKafkaPublisher> logger,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _logger = logger;

        var bootstrapServers = kafkaOptions.Value.BootstrapServers;
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("BootstrapServers не настроены в KafkaSettings.");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }


    public async Task PublishBookingCreatedAsync(BookingCreated request)
    {
        try
        {
            var result = await _producer.ProduceAsync(KafkaTopics.BookingCreated, new Message<string, string>
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
        try
        {
            var result = await _producer.ProduceAsync(KafkaTopics.BookingCancellation, new Message<string, string>
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _producer?.Dispose();
        _disposed = true;
    }

}
