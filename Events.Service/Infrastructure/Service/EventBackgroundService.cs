using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CSCourse.Contracts.Kafka;
using Events.Service.Application.Interfaces;
using Events.Service.Infrastructure.Config;

namespace Events.Service.Infrastructure.Services;

public class EventBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EventBackgroundService> _logger;
    private readonly IEventKafkaPublisher _kafkaPublisher;
    private readonly ConsumerConfig _consumerConfig;

    private readonly string _bookingCreatedTopic = KafkaTopics.BookingCreated;
    private readonly string _bookingCancellationTopic = KafkaTopics.BookingCancellation;

    public EventBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EventBackgroundService> logger,
        IEventKafkaPublisher kafkaPublisher,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _kafkaPublisher = kafkaPublisher;

        var bootstrapServers = kafkaOptions.Value.BootstrapServers;
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("BootstrapServers не настроены в KafkaSettings.");

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "event-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _logger.LogInformation(
            "Kafka consumer подготовлен: BootstrapServers={Bootstrap}, GroupId={GroupId}, Topics={Topic1},{Topic2}",
            bootstrapServers,
            _consumerConfig.GroupId,
            _bookingCreatedTopic,
            _bookingCancellationTopic);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();

        consumer.Subscribe([_bookingCreatedTopic, _bookingCancellationTopic]);

        _logger.LogInformation(
            "Kafka consumer запущен. Ожидание сообщений из топиков '{Topic1}' и '{Topic2}'...",
            _bookingCreatedTopic,
            _bookingCancellationTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message == null)
                    continue;

                var message = consumeResult.Message.Value;
                _logger.LogDebug(
                    "Получено сообщение из топика '{Topic}': {Message}",
                    consumeResult.Topic,
                    message);

                try
                {
                    if (consumeResult.Topic == _bookingCreatedTopic)
                    {
                        var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(message);
                        if (bookingCreated == null)
                        {
                            _logger.LogWarning("Сообщение BookingCreated пришло без данных.");
                            consumer.StoreOffset(consumeResult);
                            continue;
                        }

                        _logger.LogInformation(
                            "Получен запрос на бронирование [{Offset}] Id={Id}, EventId={EventId}, UserId={UserId}, Quantity={Quantity}, CreatedAt={CreatedAt}",
                            consumeResult.TopicPartitionOffset,
                            bookingCreated.Id,
                            bookingCreated.EventId,
                            bookingCreated.UserId,
                            bookingCreated.Quantity,
                            bookingCreated.CreatedAt);

                        await ProcessBookingCreated(bookingCreated);
                    }
                    else if (consumeResult.Topic == _bookingCancellationTopic)
                    {
                        var cancellation = JsonSerializer.Deserialize<BookingCancellation>(message);
                        if (cancellation == null)
                        {
                            _logger.LogWarning("Сообщение BookingCancellation пришло без данных.");
                            consumer.StoreOffset(consumeResult);
                            continue;
                        }

                        _logger.LogInformation(
                            "Получен запрос на отмену бронирования [{Offset}] Id={Id}, EventId={EventId}, CancelledAt={CancelledAt}, Reason={Reason}",
                            consumeResult.TopicPartitionOffset,
                            cancellation.Id,
                            cancellation.EventId,
                            cancellation.CancelledAt,
                            cancellation.Reason);

                        await ProcessBookingCancellation(cancellation);
                    }
                    else
                    {
                        _logger.LogWarning("Получено сообщение из неизвестного топика '{Topic}'.", consumeResult.Topic);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Ошибка десериализации сообщения: {Msg}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения из топика '{Topic}': {Msg}", consumeResult.Topic, message);
                }
                finally
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

    private async Task ProcessBookingCreated(BookingCreated bookingCreated)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        try
        {
            // Проверяем существование мероприятия
            var @event = await eventService.GetEventByIdAsync(bookingCreated.EventId);
            if (@event == null)
            {
                _logger.LogWarning(
                    "Мероприятие {EventId} не найдено при обработке бронирования {BookingId}.",
                    bookingCreated.EventId,
                    bookingCreated.Id);

                await _kafkaPublisher.PublishBookingResponseAsync(new BookingResponse
                {
                    Id = bookingCreated.Id,
                    EventId = bookingCreated.EventId,
                    ProcessedAt = DateTime.UtcNow,
                    Status = BookingResponseStatus.Rejected,
                    Message = "Event not found"
                });
                return;
            }

            // Пытаемся зарезервировать места
            bool reserved = await eventService.TryReserveSeatsAsync(
                bookingCreated.EventId,
                bookingCreated.Quantity);

            if (reserved)
            {
                _logger.LogInformation(
                    "Места для мероприятия {EventId} зарезервированы. Бронирование {BookingId} подтверждено.",
                    bookingCreated.EventId,
                    bookingCreated.Id);

                await _kafkaPublisher.PublishBookingResponseAsync(new BookingResponse
                {
                    Id = bookingCreated.Id,
                    EventId = bookingCreated.EventId,
                    ProcessedAt = DateTime.UtcNow,
                    Status = BookingResponseStatus.Confirmed,
                    Message = "Booking confirmed"
                });
            }
            else
            {
                _logger.LogWarning(
                    "Недостаточно мест для мероприятия {EventId}. Бронирование {BookingId} отклонено.",
                    bookingCreated.EventId,
                    bookingCreated.Id);

                await _kafkaPublisher.PublishBookingResponseAsync(new BookingResponse
                {
                    Id = bookingCreated.Id,
                    EventId = bookingCreated.EventId,
                    ProcessedAt = DateTime.UtcNow,
                    Status = BookingResponseStatus.Rejected,
                    Message = "Not enough available seats"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка при обработке бронирования {BookingId} для мероприятия {EventId}.",
                bookingCreated.Id,
                bookingCreated.EventId);

            await _kafkaPublisher.PublishBookingResponseAsync(new BookingResponse
            {
                Id = bookingCreated.Id,
                EventId = bookingCreated.EventId,
                ProcessedAt = DateTime.UtcNow,
                Status = BookingResponseStatus.Error,
                Message = $"Error in EventService: {ex.Message}"
            });
        }
    }

    private async Task ProcessBookingCancellation(BookingCancellation cancellation)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        try
        {
            var @event = await eventService.GetEventByIdAsync(cancellation.EventId);
            if (@event == null)
            {
                _logger.LogWarning(
                    "Мероприятие {EventId} не найдено при обработке отмены бронирования {BookingId}.",
                    cancellation.EventId,
                    cancellation.Id);
                return;
            }

            bool released = await eventService.ReleaseSeatsAsync(cancellation.EventId);

            if (released)
            {
                _logger.LogInformation(
                    "Места для мероприятия {EventId} освобождены. Бронирование {BookingId} отменено.",
                    cancellation.EventId,
                    cancellation.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Не удалось освободить места для мероприятия {EventId} при отмене бронирования {BookingId}.",
                    cancellation.EventId,
                    cancellation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка при обработке отмены бронирования {BookingId} для мероприятия {EventId}.",
                cancellation.Id,
                cancellation.EventId);
        }
    }
}
