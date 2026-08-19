using Bookings.Service.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CSCourse.Contracts.Kafka; // Твои контракты событий
using System.Threading;
using CSCourse.Contracts.Exceptions;
using Bookings.Service.Domain.Models;

namespace Bookings.Service.Application.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBookingKafkaConsumer _kafkaConsumer;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IBookingKafkaConsumer kafkaConsumer,
        ILogger<BookingBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaConsumer = kafkaConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingBackgroundService starting Kafka consumption...");

        try
        {
            // Запускаем цикл чтения сообщений из Kafka
            await _kafkaConsumer.StartConsumingAsync(stoppingToken);
            
            // Если метод вернулся (например, при остановке), ждем
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BookingBackgroundService cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "BookingBackgroundService crashed.");
            throw;
        }
    }

    // Вспомогательный метод для обработки конкретного события
    // Вызывается внутри IBookingKafkaConsumer при получении сообщения
    public async Task HandleBookingEventAsync(BookingConfirmed eventMessage, CancellationToken token)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogInformation(
            "Processing confirmation for booking {BookingId} from event {EventId}", 
            eventMessage.BookingId, eventMessage.EventId);

        try
        {
            // Обновляем статус в БД на основе входящего события
            await bookingService.UpdateBookingStatusAsync(
                eventMessage.BookingId, 
                BookingStatus.Confirmed, 
                message: "Confirmed by Event Service");
            
            _logger.LogInformation("Booking {BookingId} successfully confirmed.", eventMessage.BookingId);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Booking {BookingId} not found when processing confirmation. It might have been deleted.", eventMessage.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update booking {BookingId} status.", eventMessage.BookingId);
            // Здесь можно добавить логику повторной попытки (retry) или Dead Letter Queue
        }
    }

    public async Task HandleBookingRejectionAsync(BookingRejected eventMessage, CancellationToken token)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogWarning(
            "Processing rejection for booking {BookingId}. Reason: {Reason}", 
            eventMessage.BookingId, eventMessage.Reason);

        try
        {
            // Отменяем бронь (статус Rejected)
            await bookingService.UpdateBookingStatusAsync(
                eventMessage.BookingId, 
                BookingStatus.Rejected, 
                message: eventMessage.Reason ?? "Rejected by Event Service");

            _logger.LogInformation("Booking {BookingId} rejected.", eventMessage.BookingId);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Booking {BookingId} not found when processing rejection.", eventMessage.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update booking {BookingId} rejection status.", eventMessage.BookingId);
        }
    }
}
