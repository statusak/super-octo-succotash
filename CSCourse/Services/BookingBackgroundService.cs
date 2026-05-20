using CSCourse.Interfaces;
using CSCourse.Models;

namespace CSCourse.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly IBookingTaskQueue _bookingTaskQueue;
        private readonly ILogger<BookingBackgroundService> _logger;

        private readonly TimeSpan _periodicTimer;

        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        public BookingBackgroundService(
            IBookingService bookingService,
            IEventService eventService,
            IBookingTaskQueue bookingTaskQueue,
            ILogger<BookingBackgroundService> logger,
            TimeSpan? periodicTimer = null
            )
        {
            _bookingService = bookingService;
            _eventService = eventService;
            _bookingTaskQueue = bookingTaskQueue;
            _logger = logger;
            _periodicTimer = periodicTimer ?? TimeSpan.FromMinutes(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingBackgroundService startup");
            using var timer = new PeriodicTimer(_periodicTimer);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    List<Booking> pendingBookings = _bookingService.GetPending().ToList();
                    if (pendingBookings.Any())
                    {
                        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
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

        async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                if (!_eventService.IsEventExists(booking.EventId))
                {
                    _logger.LogInformation("EventId {EventId} did not exists", booking.EventId);
                    await _bookingService.UpdateProcessedBookingByIdAsync(booking.Id, new BookingProcessedDto { Status = BookingStatus.Rejected, ProcessedAt = DateTime.UtcNow });
                    return;
                }

                if (!_eventService.TryReserveSeats(booking.EventId))
                {
                    _logger.LogInformation("EventId {EventId} did not exists needed seats", booking.EventId);
                    await _bookingService.UpdateProcessedBookingByIdAsync(booking.Id, new BookingProcessedDto { Status = BookingStatus.Rejected, ProcessedAt = DateTime.UtcNow });
                    return;
                }

                _logger.LogInformation(
                            "Processing bookingId {TaskId} for eventId {EventId}, whitch created at {CreatedAt}",
                            booking.Id, booking.EventId, booking.CreatedAt);

                await _bookingService.UpdateProcessedBookingByIdAsync(booking.Id, new BookingProcessedDto { Status = BookingStatus.Confirmed, ProcessedAt = DateTime.UtcNow });
                _logger.LogInformation(
                    "Booking {TaskId} success processed", booking.Id);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
    }
}
