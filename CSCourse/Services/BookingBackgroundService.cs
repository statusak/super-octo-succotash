using CSCourse.Interfaces;
using CSCourse.Models;

namespace CSCourse.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IBookingService _bookingService;
        private readonly IBookingTaskQueue _bookingTaskQueue;
        private readonly ILogger<BookingBackgroundService> _logger;

        public BookingBackgroundService(
            IBookingService bookingService,
            IBookingTaskQueue bookingTaskQueue,
            ILogger<BookingBackgroundService> logger)
        {
            _bookingService = bookingService;
            _bookingTaskQueue = bookingTaskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingBackgroundService startup");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_bookingTaskQueue.TryDequeue(out var task))
                    {
                        if(task == null)
                            continue;

                        _logger.LogInformation(
                            "Processing bookingId {TaskId} for eventId {EventId}, whitch created at {CreatedAt}",
                            task.Id, task.EventId, task.CreatedAt);

                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                        await _bookingService.UpdateProcessedBookingByIdAsync(task.Id, new BookingProcessedDto { Status = BookingStatus.Confirmed, ProcessedAt = DateTime.UtcNow });

                        _logger.LogInformation(
                            "Booking {TaskId} success processed", task.Id);
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

    }
}
