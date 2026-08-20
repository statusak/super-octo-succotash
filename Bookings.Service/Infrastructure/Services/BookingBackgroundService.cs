using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CSCourse.Contracts.Kafka;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Domain.Models;
using Bookings.Service.Infrastructure.Config;

namespace Bookings.Service.Infrastructure.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topicName;

    public BookingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BookingBackgroundService> logger,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        var bootstrapServers = kafkaOptions.Value.BootstrapServers;
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("BootstrapServers не настроены в KafkaSettings.");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = kafkaOptions.Value.GroupId ?? "booking-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // ручное подтверждение смещения
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _topicName = KafkaTopics.BookingResponse;

        _logger.LogInformation(
            "Kafka consumer подготовлен: BootstrapServers={Bootstrap}, GroupId={GroupId}, Topic={Topic}",
            bootstrapServers,
            consumerConfig.GroupId,
            _topicName);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume — блокирующий вызов, поэтому выносим в отдельный поток,
        // чтобы не блокировать запуск хоста.
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private void Consume(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topicName);

        _logger.LogInformation("Kafka consumer запущен. Ожидание сообщений из топика '{Topic}'...", _topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult?.Message == null)
                    continue;

                var message = consumeResult.Message.Value;
                _logger.LogDebug("Получено сообщение: {Message}", message);

                try
                {
                    // Сначала читаем тип события, чтобы понять, как десериализовать
                    using var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;
                    string eventType = root.TryGetProperty("EventType", out var typeProp)
                        ? typeProp.GetString()
                        : "Unknown";

                    if (eventType == "BookingConfirmed")
                    {
                        var confirmed = JsonSerializer.Deserialize<BookingConfirmed>(message);
                        ProcessBookingConfirmed(confirmed, consumeResult);
                    }
                    else if (eventType == "BookingRejected")
                    {
                        var rejected = JsonSerializer.Deserialize<BookingRejected>(message);
                        ProcessBookingRejected(rejected, consumeResult);
                    }
                    else
                    {
                        _logger.LogWarning("Неизвестный тип события: {Type}. Сообщение будет пропущено.", eventType);
                        _consumer.StoreOffset(consumeResult);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Ошибка десериализации сообщения: {Msg}", message);
                    _consumer.StoreOffset(consumeResult); // в учебном проекте просто пропускаем
                }
            }
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Ошибка при потреблении из Kafka.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer остановлен штатно.");
        }
    }

    // Dispose для корректного закрытия consumer (BackgroundService реализует IDisposable)
    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }

    private void ProcessBookingConfirmed(BookingConfirmed? eventMessage, ConsumeResult<string, string> consumeResult)
    {
        if (eventMessage == null)
        {
            _logger.LogWarning("Сообщение BookingConfirmed пришло без данных.");
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogInformation(
            "Обработка подтверждения бронирования: BookingId={BookingId}, EventId={EventId}",
            eventMessage.BookingId,
            eventMessage.EventId);

        try
        {
            bookingService.UpdateBookingStatusAsync(
                eventMessage.BookingId,
                BookingStatus.Confirmed,
                message: "Confirmed by Event Service")
                .GetAwaiter().GetResult();

            _logger.LogInformation("Бронирование {BookingId} успешно подтверждено.", eventMessage.BookingId);
            _consumer.StoreOffset(consumeResult);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning(
                "Бронирование {BookingId} не найдено при обработке подтверждения. Возможно, оно было удалено.",
                eventMessage.BookingId);
            _consumer.StoreOffset(consumeResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Не удалось обновить статус бронирования {BookingId}.",
                eventMessage.BookingId);
            // В учебном проекте коммитим, чтобы не блокировать топик
            _consumer.StoreOffset(consumeResult);
        }
    }

    private void ProcessBookingRejected(BookingRejected? eventMessage, ConsumeResult<string, string> consumeResult)
    {
        if (eventMessage == null)
        {
            _logger.LogWarning("Сообщение BookingRejected пришло без данных.");
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogWarning(
            "Обработка отклонения бронирования: BookingId={BookingId}, Причина={Reason}",
            eventMessage.BookingId,
            eventMessage.Reason);

        try
        {
            bookingService.UpdateBookingStatusAsync(
                eventMessage.BookingId,
                BookingStatus.Rejected,
                message: eventMessage.Reason ?? "Rejected by Event Service")
                .GetAwaiter().GetResult();

            _logger.LogInformation("Бронирование {BookingId} отклонено.", eventMessage.BookingId);
            _consumer.StoreOffset(consumeResult);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning(
                "Бронирование {BookingId} не найдено при обработке отклонения.",
                eventMessage.BookingId);
            _consumer.StoreOffset(consumeResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Не удалось обновить статус отклонения бронирования {BookingId}.",
                eventMessage.BookingId);
            _consumer.StoreOffset(consumeResult);
        }
    }
}
