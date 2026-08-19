using System.Text;
using System.Threading;
using Confluent.Kafka;
using CSCourse.Contracts.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bookings.Service.Infrastructure.Kafka;

public class BookingKafkaPublisher : IBookingKafkaPublisher
{
    private readonly ProducerConfig _producerConfig;
    private readonly string _createdTopic;
    private readonly string _cancellationTopic;
    private readonly ILogger<BookingKafkaPublisher> _logger;

    public BookingKafkaPublisher(
        ILogger<BookingKafkaPublisher> logger,
        string bootstrapServers = "localhost:9092",
        string createdTopic = "booking.created",
        string cancellationTopic = "booking.cancellation")
    {
        _logger = logger;
        _createdTopic = createdTopic;
        _cancellationTopic = cancellationTopic;

        _producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            // Для продакшена лучше включить acks=all и retries, но для учебного проекта можно оставить по умолчанию
            EnableDeliveryReport = true 
        };
    }

    public async Task PublishBookingCreatedAsync(BookingCreated request)
    {
        var message = new Message<string, string>
        {
            Key = request.BookingId.ToString(), // Ключ важен для упорядочивания по брони
            Value = Serialize(request),
            TopicName = _createdTopic
        };

        await ProduceAsync(message, "BookingCreated");
    }

    public async Task PublishBookingCancellationAsync(BookingCancellation request)
    {
        var message = new Message<string, string>
        {
            Key = request.BookingId.ToString(),
            Value = Serialize(request),
            TopicName = _cancellationTopic
        };

        await ProduceAsync(message, "BookingCancellation");
    }

    private string Serialize(object obj)
    {
        // Используем Newtonsoft.Json, как в твоём Consumer
        return JsonConvert.SerializeObject(obj, Formatting.None);
    }

    private async Task ProduceAsync(Message<string, string> message, string operationName)
    {
        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            var deliveryReport = await producer.ProduceAsync(message);
            _logger.LogInformation(
                "{Operation} published to topic '{Topic}' with offset {Offset} on partition {Partition}",
                operationName,
                deliveryReport.Topic,
                deliveryReport.Offset,
                deliveryReport.Partition);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "{Operation} failed to publish to topic '{Topic}': {Error}",
                operationName,
                message.TopicName,
                ex.Error.Reason);

            throw; // Пробрасываем дальше, чтобы BookingService мог решить, что делать (например, откатить транзакцию)
        }
    }
}
