using Bookings.Service.Domain.Models;
using Bookings.Service.Application.Interfaces;
using CSCourse.Contracts.Models;
using CSCourse.Contracts.Kafka;
using CSCourse.Contracts.Exceptions;
using Bookings.Service.Application.Models;

namespace Bookings.Service.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingKafkaPublisher _kafkaPublisher;
    private readonly IBookingRepository _bookings;
    private readonly SemaphoreSlim _processingSemaphoreBooking = new(1, 1);

    public BookingService(
        IBookingKafkaPublisher kafkaPublisher,
        IBookingRepository bookings)
    {
        _kafkaPublisher = kafkaPublisher;
        _bookings = bookings;
    }
    
    public async Task<Booking> InitiateBookingAsync(Guid eventId, Guid userId)
    {
        await _processingSemaphoreBooking.WaitAsync();
        try
        {
            BookingRepositoryCreateDto dto = new BookingRepositoryCreateDto{
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Сохраняем бронь в БД через репозиторий
            Booking booking = await _bookings.CreateAsync(dto);

            // Публикуем событие в Kafka
            await _kafkaPublisher.PublishBookingCreatedAsync(new BookingCreated
            {
                Id = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Quantity = 1,
                CreatedAt = booking.CreatedAt
            });

            return booking;
        }
        finally
        {
            _processingSemaphoreBooking.Release();
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, Guid userId, AccountRole role)
    {
        var booking = await _bookings.GetByIdAsync(bookingId);
        if (booking == null)
            throw new NotFoundException($"Booking with id {bookingId} not found");

        if (role != AccountRole.Admin && booking.UserId != userId)
            throw new UnauthorizedOperationException("You can only access your own bookings");

        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, AccountRole role)
    {
        // Race condition defender
        await _processingSemaphoreBooking.WaitAsync();
        try
        {
            var booking = await _bookings.GetByIdAsync(bookingId);
            if (booking == null)
                throw new NotFoundException($"Booking with id {bookingId} not found");

            if (role != AccountRole.Admin && booking.UserId != userId)
                throw new UnauthorizedOperationException("You can only cancel your own bookings");

            if (booking.Status != BookingStatus.Confirmed)
                throw new InvalidOperationException("Can cancel only confirmed booking");

            BookingRepositoryUpdateDto dto = new BookingRepositoryUpdateDto
            {
                Id = booking.Id,
                Status = BookingStatus.Cancelled,
                ProcessedAt = DateTime.UtcNow
            };

            await _bookings.UpdateAsync(dto);

            await _kafkaPublisher.PublishBookingCancellationAsync(new BookingCancellation
            {
                Id = booking.Id,
                EventId = booking.EventId,
                CancelledAt = dto.ProcessedAt,
                Reason = "User requested cancellation"
            });
        }
        finally
        {
            _processingSemaphoreBooking.Release();
        }
    }
    public async Task UpdateBookingStatusAsync(Guid bookingId, BookingStatus status, string? message = null)
    {
        await _processingSemaphoreBooking.WaitAsync();
        try
        {
            var booking = await _bookings.GetByIdAsync(bookingId);
            if (booking == null)
                throw new NotFoundException($"Booking with id {bookingId} not found");

            BookingRepositoryUpdateDto dto = new BookingRepositoryUpdateDto
            {
                Id = booking.Id,
                Status = status,
                ProcessedAt = DateTime.UtcNow
            };

            await _bookings.UpdateAsync(dto);
        }
        finally
        {
            _processingSemaphoreBooking.Release();
        }
    }

    /// <summary>
    /// Возвращает список бронирований со статусом Pending для фоновой обработки.
    /// </summary>
    public async Task<IEnumerable<Booking>> GetPendingAsync()
    {
        return await _bookings.GetPendingAsync();
    }
}
