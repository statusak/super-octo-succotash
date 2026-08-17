using Bookings.Service.Domain.Models;
using Bookings.Service.Domain.Exceptions;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Application.Models;

namespace Bookings.Service.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingKafkaPublisher _kafkaPublisher;

        private readonly IBookingRepository _bookings;

        private readonly SemaphoreSlim _processingSemaphoreBooking = new(1, 1);

        public BookingService(
            IBookingKafkaPublisher kafkaPublisher, IBookingRepository bookings)
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

                // Отправляем событие в Kafka
                await _kafkaPublisher.PublishBookingCreatedAsync(new BookingCreatedRequest
                {
                    Id = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
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
            Booking? booking;
            // TODO: Здесь было-бы уместно использовать транзакцию
            await _processingSemaphoreBooking.WaitAsync();
            try
            {
                booking = await _bookings.GetByIdAsync(bookingId);

                if(booking == null)
                {
                    throw new NotFoundException($"not found booking with id {bookingId}");
                }

                if(booking.Status == BookingStatus.Rejected || booking.Status == BookingStatus.Cancelled)
                {
                    throw new BookingAlreadyCancelledException();
                }

                if(role != AccountRole.Admin)
                {
                    if(userId != booking.UserId)
                    {
                        throw new UnauthorizedOperationException($"You can not canceled booking with id {bookingId}");
                    }   
                }

                BookingProcessedDto bookingProcessedDto = new BookingProcessedDto
                {
                    Status = BookingStatus.Cancelled,
                    ProcessedAt = DateTime.UtcNow,
                };

                if(await UpdateProcessedBookingByIdAsync(bookingId, bookingProcessedDto))
                {
                    return await _eventService.ReleaseSeatsAsync(booking.EventId);
                } else
                {
                    // TODO: Сделать лучшую архитектуру, для понимания почему не отменилась бронь
                    return false;
                }
            }
            finally
            {
                _processingSemaphoreBooking.Release();
            }
        }

        public IEnumerable<Booking> GetPending()
        {
            var pendingBooking = _bookings.GetPending();
            return pendingBooking;
        }

        public async Task<IEnumerable<Booking>> GetPendingAsync()
        {
            var pendingBooking = await _bookings.GetPendingAsync();
            return pendingBooking;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _bookings.GetByIdAsync(bookingId);
        }
        public async Task<bool> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking)
        {
            var bookingsRepositoryUpdateDto = new BookingRepositoryUpdateDto
            {
                Id = bookingId,
                Status = booking.Status,
                ProcessedAt = booking.ProcessedAt,
            };
            return await _bookings.UpdateAsync(bookingsRepositoryUpdateDto); 
        }
    }
}
