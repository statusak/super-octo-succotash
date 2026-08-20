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
using CSCourse.Contracts.Exceptions;

namespace Bookings.Service.Infrastructure.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly IBookingKafkaPublisher<BookingKafkaPublisher> _kafkaPublisher;
    private readonly string _topicName = KafkaTopics.BookingResponse;

    private readonly ConsumerConfig _consumerConfig;

    public BookingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BookingBackgroundService> logger,
        IBookingKafkaPublisher<BookingKafkaPublisher> kafkaPublisher,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaPublisher = kafkaPublisher;
        _logger = logger;

        var bootstrapServers = kafkaOptions.Value.BootstrapServers;
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("BootstrapServers не настроены в KafkaSettings.");

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "booking-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // ручное подтверждение смещения
        };

        _topicName = KafkaTopics.BookingResponse;

        _logger.LogInformation(
            "Kafka consumer подготовлен: BootstrapServers={Bootstrap}, GroupId={GroupId}, Topic={Topic}",
            bootstrapServers,
            _consumerConfig.GroupId,
            _topicName);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume — блокирующий вызов, поэтому выносим в отдельный поток,
        // чтобы не блокировать запуск хоста.
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(_topicName);

        _logger.LogInformation("Kafka consumer запущен. Ожидание сообщений из топика '{Topic}'...", _topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message == null)
                    continue;

                var message = consumeResult.Message.Value;
                _logger.LogDebug("Получено сообщение: {Message}", message);

                try
                {
                    var eventMessage = JsonSerializer.Deserialize<BookingResponse>(consumeResult.Message.Value);

                    if (eventMessage == null)
                    {
                        _logger.LogWarning("Сообщение BookingConfirmed пришло без данных.");
                        return;
                    }


                    _logger.LogInformation(
                        "Получен ответ на бронирование [{Offset}] Id={Id}, ProcessedAt={ProcessedAt}, Status={Status}, Message={Message}",
                        consumeResult.TopicPartitionOffset,
                        eventMessage.Id,
                        eventMessage?.ProcessedAt,
                        eventMessage?.Status, 
                        eventMessage?.Message);

                    switch (eventMessage.Status)
                    {
                        case BookingResponseStatus.Confirmed:
                            await ProcessBookingConfirmed(eventMessage);
                            break;
                        case BookingResponseStatus.Rejected:
                            await ProcessBookingRejected(eventMessage);
                            break;
                        case BookingResponseStatus.Error:
                            await ProcessBookingError(eventMessage);
                            break;  
                        default:
                            break;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Ошибка десериализации сообщения: {Msg}", message);
                    consumer.StoreOffset(consumeResult);
                } finally
                {
                    consumer.StoreOffset(consumeResult);
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

    private async Task ProcessBookingConfirmed(BookingResponse eventMessage)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogInformation("Обработка подтверждения бронирования: BookingId={BookingId}", eventMessage.Id);

        try
        {
            await bookingService.UpdateBookingStatusAsync(eventMessage.Id, BookingStatus.Confirmed);

            _logger.LogInformation("Бронирование {BookingId} успешно подтверждено.", eventMessage.Id);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Бронирование {BookingId} не найдено при обработке подтверждения. Возможно, оно было удалено.",
                                                                                                             eventMessage.Id);
            await _kafkaPublisher.PublishBookingCancellationAsync(new BookingCancellation
            {
                Id = eventMessage.Id,
                EventId = eventMessage.EventId,
                CancelledAt = eventMessage.ProcessedAt,
                Reason = "Error on UpdateBookingStatusAsync: NotFoundException"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обновить статус бронирования {BookingId}.", eventMessage.Id);
            await _kafkaPublisher.PublishBookingCancellationAsync(new BookingCancellation
            {
                Id = eventMessage.Id,
                EventId = eventMessage.EventId,
                CancelledAt = eventMessage.ProcessedAt,
                Reason = $"Error on UpdateBookingStatusAsync: {ex.Message}"
            });
        }
    }

    private async Task ProcessBookingRejected(BookingResponse eventMessage)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogWarning("Обработка отклонения бронирования: BookingId={BookingId}, Причина={Reason}",
                                                                 eventMessage.Id, eventMessage.Message);
        try
        {
            await bookingService.UpdateBookingStatusAsync(eventMessage.Id, BookingStatus.Rejected);
            _logger.LogInformation("Бронирование {BookingId} отклонено.", eventMessage.Id);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Бронирование {BookingId} не найдено при обработке отклонения.", eventMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обновить статус отклонения бронирования {BookingId}.", eventMessage.Id);
        }
    }

    private async Task ProcessBookingError(BookingResponse eventMessage)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogWarning("Обработка отклонения бронирования: BookingId={BookingId}, Причина={Reason}",
                                                                 eventMessage.Id, eventMessage.Message);
        try
        {
            await bookingService.UpdateBookingStatusAsync(eventMessage.Id, BookingStatus.Rejected);
            _logger.LogInformation("Бронирование {BookingId} отклонено.", eventMessage.Id);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Бронирование {BookingId} не найдено при обработке отклонения.", eventMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обновить статус отклонения бронирования {BookingId}.", eventMessage.Id);
        }
    }
}
