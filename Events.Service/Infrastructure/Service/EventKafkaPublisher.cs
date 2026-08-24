using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CSCourse.Contracts.Kafka;
using Events.Service.Application.Interfaces;
using Events.Service.Infrastructure.Config;

namespace Events.Service.Infrastructure.Services;

public class EventKafkaPublisher : IEventKafkaPublisher
{
    private readonly ILogger<EventKafkaPublisher> _logger;
    private readonly IProducer<string, string> _producer;
    private readonly string _topicName = KafkaTopics.BookingResponse;

    public EventKafkaPublisher(
        ILogger<EventKafkaPublisher> logger,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _logger = logger;

        var bootstrapServers = kafkaOptions.Value.BootstrapServers;
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("BootstrapServers не настроены в KafkaSettings.");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5
        };

        _producer = new ProducerBuilder<string, string>(config).Build();

        _logger.LogInformation(
            "Kafka producer подготовлен: BootstrapServers={Bootstrap}, Topic={Topic}",
            bootstrapServers,
            _topicName);
    }

    public async Task PublishBookingResponseAsync(BookingResponse response)
    {
        var key = response.Id.ToString();
        var value = JsonSerializer.Serialize(response);

        try
        {
            var result = await _producer.ProduceAsync(_topicName, new Message<string, string>
            {
                Key = key,
                Value = value
            });

            _logger.LogInformation(
                "BookingResponse отправлен: Id={Id}, Status={Status}, Topic={Topic}, Partition={Partition}, Offset={Offset}",
                response.Id,
                response.Status,
                result.Topic,
                result.Partition,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Ошибка при отправке BookingResponse: Id={Id}", response.Id);
            throw;
        }
    }
}
