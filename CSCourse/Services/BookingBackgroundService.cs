using CSCourse.Interfaces;
using CSCourse.Models;

namespace CSCourse.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BookingBackgroundService> _logger;

        private readonly TimeSpan _periodicTimer;
        private const int DefaultPollingIntervalSec = 5;

        public BookingBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BookingBackgroundService> logger,
            TimeSpan? periodicTimer = null
            )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _periodicTimer = periodicTimer ?? TimeSpan.FromSeconds(DefaultPollingIntervalSec);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingBackgroundService startup");
            using var timer = new PeriodicTimer(_periodicTimer);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    List<Guid> pendingBookingIds;
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        pendingBookingIds = (await bookingService.GetPendingAsync())
                            .Select(b => b.Id)
                            .ToList();
                    }

                    if (pendingBookingIds.Any())
                    {
                        var tasks = pendingBookingIds.Select(bookingId => ProcessBookingAsync(bookingId, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("BookingBackgroundService catch interrupt");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error while processing booking");
                }
            }
           

            _logger.LogInformation("BookingBackgroundService stop");
        }

        async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            using(var scope = _serviceScopeFactory.CreateScope())
            {
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                try
                {
                    var booking = await bookingService.GetBookingByIdAsync(bookingId);
                    if (booking == null)
                    {
                        _logger.LogError("BookingId {BookingId} did not exists", bookingId);
                        return;
                    }

                    if(!await eventService.IsEventExistsAsync(booking.EventId))
                    {
                        _logger.LogError("EventId {EventId} did not exists", bookingId);
                        await bookingService.UpdateProcessedBookingByIdAsync(booking.Id, new BookingProcessedDto { Status = BookingStatus.Rejected, ProcessedAt = DateTime.UtcNow });
                        return;
                    }

                    _logger.LogInformation(
                                "Processing bookingId {TaskId} for eventId {EventId}, whitch created at {CreatedAt}",
                                booking.Id, booking.EventId, booking.CreatedAt);

                    await bookingService.UpdateProcessedBookingByIdAsync(booking.Id, new BookingProcessedDto { Status = BookingStatus.Confirmed, ProcessedAt = DateTime.UtcNow });
                    _logger.LogInformation(
                        "Booking {TaskId} success processed", booking.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("BookingBackgroundService catch interrupt");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing booking {BookingId}", bookingId);
                    try
                    {
                        await bookingService.UpdateProcessedBookingByIdAsync(
                            bookingId,
                            new BookingProcessedDto
                            {
                                Status = BookingStatus.Rejected,
                                ProcessedAt = DateTime.UtcNow
                            });

                        var booking = await bookingService.GetBookingByIdAsync(bookingId);
                        if (booking == null)
                        {
                            _logger.LogError(e, "BookingId {BookingId} did not exists", bookingId);
                            return;
                        }

                        if (await eventService.ReleaseSeatsAsync(booking.EventId))
                        {
                            _logger.LogInformation(
                                "EventId {EventId} success release seats for bookingId {BookingId}",
                                booking.EventId, bookingId);
                        }
                        else
                        {
                            _logger.LogError(
                                "EventId {EventId} error release seats for bookingId {BookingId}",
                                booking.EventId, bookingId);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx,
                            "Failed to update booking {BookingId} status after error", bookingId);
                    }
                }
            }
        }
    }
}
