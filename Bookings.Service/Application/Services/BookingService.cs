using Bookings.Service.Domain.Models;
using Bookings.Service.Domain.Exceptions;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Application.Models;
using CSCourse.Contracts.Models;
using CSCourse.Contracts.Exceptions;

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

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _processingSemaphoreBooking.WaitAsync();
        try
        {
            var dto = new BookingRepositoryCreateDto
            {
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var bookingId = await _bookings.CreateAsync(dto);

            var booking = new Booking
            {
                Id = bookingId,
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = dto.CreatedAt
            };

            await _kafkaPublisher.PublishBookingCreatedAsync(new BookingCreatedRequest
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

    public async Task<bool> CancelledBookingByIdAsync(Guid bookingId, Guid userId, AccountRole role)
    {
        await _processingSemaphoreBooking.WaitAsync();
        try
        {
            var booking = await _bookings.GetByIdAsync(bookingId);
            if (booking == null) throw new NotFoundException(...);
            if (booking.Status is BookingStatus.Rejected or BookingStatus.Cancelled)
                throw new BookingAlreadyCancelledException();

            if (role != AccountRole.Admin && userId != booking.UserId)
                throw new UnauthorizedOperationException(...);

            var dto = new BookingProcessedDto
            {
                Status = BookingStatus.Cancelled,
                ProcessedAt = DateTime.UtcNow,
            };

            if (!await UpdateProcessedBookingByIdAsync(bookingId, dto))
                return false;

            // ТОЛЬКО публикация события. Никаких вызовов Event.Service
            await _kafkaPublisher.PublishBookingCancelledAsync(new BookingCancelledEvent
            {
                Id = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                CancelledAt = DateTime.UtcNow
            });

            return true;
        }
        finally
        {
            _processingSemaphoreBooking.Release();
        }
    }
    public IEnumerable<Booking> GetPending()
    {
        return _bookings.GetPending();
    }

    public async Task<IEnumerable<Booking>> GetPendingAsync()
    {
        return await _bookings.GetPendingAsync();
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        return await _bookings.GetByIdAsync(bookingId);
    }

    public async Task<bool> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking)
    {
        var dto = new BookingRepositoryUpdateDto
        {
            Id = bookingId,
            Status = booking.Status,
            ProcessedAt = booking.ProcessedAt,
        };
        return await _bookings.UpdateAsync(dto);
    }
}
