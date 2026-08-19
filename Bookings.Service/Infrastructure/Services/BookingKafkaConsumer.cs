using Confluent.Kafka;
using Bookings.Service.Application.Interfaces;
using CSCourse.Contracts.Kafka;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Bookings.Service.Application.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookings.Service.Infrastructure.Kafka;

public class BookingKafkaConsumer : IBookingKafkaConsumer
{
    private readonly BookingBackgroundService _backgroundService; // Инжектим сервис, чтобы вызвать его методы
    private readonly ILogger<BookingKafkaConsumer> _logger;
    private const string TopicName = "booking.responses"; // Топик, куда Event.Service пишет ответы

    public BookingKafkaConsumer(
        BookingBackgroundService backgroundService,
        ILogger<BookingKafkaConsumer> logger)
    {
        _backgroundService = backgroundService;
        _logger = logger;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092", // Замени на конфиг из appsettings
            GroupId = "booking-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // Мы будем коммитить вручную после обработки
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicName);

        _logger.LogInformation("Started consuming from topic: {Topic}", TopicName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                if (consumeResult?.Message != null)
                {
                    var message = consumeResult.Message.Value;
                    _logger.LogDebug("Received message: {Message}", message);

                    // Десериализация. Предполагаем, что Event.Service отправляет JSON
                    // Лучше использовать один базовый класс или проверять тип сообщения
                    try 
                    {
                        // Вариант 1: Если Event.Service шлет разные типы, нужно определить тип
                        // Для простоты предположим, что мы можем проверить наличие поля "Status"
                        
                        dynamic obj = JsonConvert.DeserializeObject(message);
                        string eventType = obj?["EventType"]?.ToString() ?? "Unknown";

                        if (eventType == "BookingConfirmed")
                        {
                            var confirmed = JsonContent.DeserializeObject<BookingConfirmed>(message);
                            await _backgroundService.HandleBookingEventAsync(confirmed, cancellationToken);
                        }
                        else if (eventType == "BookingRejected")
                        {
                            var rejected = JsonConverter.DeserializeObject<BookingRejected>(message);
                            await _backgroundService.HandleBookingRejectionAsync(rejected, cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("Unknown event type received: {Type}", eventType);
                        }

                        // Коммитим смещение только после успешной обработки
                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize Kafka message: {Msg}", message);
                        // Решить: пропустить сообщение или отправить в DLQ
                        consumer.Commit(consumeResult); 
                    }
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Consume error");
                // Логика обработки ошибок Kafka
            }
        }
    }

    public Task StopConsumingAsync()
    {
        // Логика остановки (если нужна отдельно)
        return Task.CompletedTask;
    }
}
