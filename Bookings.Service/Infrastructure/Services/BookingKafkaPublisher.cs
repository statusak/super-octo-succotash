using System.Text.Json;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Infrastructure.Config;
using Confluent.Kafka;
using CSCourse.Contracts.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookings.Service.Infrastructure.Services;

/// <summary>
/// Издатель сообщений в Kafka для событий бронирования.
/// Реализует интерфейс <see cref="IBookingKafkaPublisher"/>.
/// Управляет жизненным циклом продюсера и публикует события BookingCreated и BookingCancellation.
/// </summary>
public class BookingKafkaPublisher : IBookingKafkaPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<BookingKafkaPublisher> _logger;
    private bool _disposed;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingKafkaPublisher"/>.
    /// </summary>
    /// <param name="logger">Логгер для записи событий и ошибок публикации.</param>
    /// <param name="kafkaOptions">Настройки Kafka, содержащие <see cref="KafkaSettings.BootstrapServers"/>.</param>
    /// <exception cref="InvalidOperationException">Выбрасывается, если BootstrapServers не заданы.</exception>
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

    /// <summary>
    /// Публикует событие создания бронирования в топик <see cref="KafkaTopics.BookingCreated"/>.
    /// </summary>
    /// <param name="request">Данные события <see cref="BookingCreated"/>.</param>
    /// <exception cref="ProduceException{TKey,TValue}">Пробрасывается при ошибке публикации в Kafka.</exception>
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
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "BookingCreated failed to publish to topic '{Topic}': {Error}",
                KafkaTopics.BookingCreated, ex.Error.Reason);

            throw;
        }
    }

    /// <summary>
    /// Публикует событие отмены бронирования в топик <see cref="KafkaTopics.BookingCancellation"/>.
    /// </summary>
    /// <param name="request">Данные события <see cref="BookingCancellation"/>.</param>
    /// <exception cref="ProduceException{TKey,TValue}">Пробрасывается при ошибке публикации в Kafka.</exception>
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
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "BookingCancellation failed to publish to topic '{Topic}': {Error}",
                KafkaTopics.BookingCancellation, ex.Error.Reason);

            throw;
        }
    }

    /// <summary>
    /// Освобождает неуправляемые ресурсы, используемые продюсером Kafka.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _producer?.Dispose();
        _disposed = true;
    }
}
