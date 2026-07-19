using CSCourse.Domain.Models;
using CSCourse.Domain.Exceptions;
using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;

namespace CSCourse.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;

        private readonly IBookingRepository _bookings;

        private readonly SemaphoreSlim _processingSemaphoreBooking = new(1, 1);

        public BookingService(
            IEventService eventService, IBookingRepository bookings)
        {
            _eventService = eventService;
            _bookings = bookings;
        }
        public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
        {
            bool canReserveSeats;
            // TODO: Здесь было-бы уместно использовать транзакцию
            await _processingSemaphoreBooking.WaitAsync();
            try
            {
                try
                {
                    int bookingCountOnEventByUser = await _bookings.GetCountBookingsOnEventByUserAsync(eventId, userId);
                    if(bookingCountOnEventByUser > 10)
                    {
                        throw new ActiveBookingsLimitExceededException($"Get limit booking for user on event: {eventId}");
                    }
                    canReserveSeats = await _eventService.TryReserveSeatsAsync(eventId);
                }
                catch (BookingForPastEventException) 
                {
                    throw;
                }
                catch (InvalidOperationException) 
                {
                    throw new NotFoundException($"not found event with id {eventId}");
                }
                catch
                {
                    throw;
                }
                if (canReserveSeats)
                {
                    var newBookingDto = new BookingRepositoryCreateDto
                    {
                        EventId = eventId,
                        UserId = userId,
                        Status = BookingStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    Guid bookingId = await _bookings.CreateAsync(newBookingDto);

                    var newBooking = new Booking
                    {
                        Id = bookingId,
                        UserId = userId, 
                        EventId = newBookingDto.EventId,
                        Status = newBookingDto.Status,
                        CreatedAt = newBookingDto.CreatedAt,
                    };

                    return newBooking;
                }

                    throw new NoAvailableSeatsException("No available seats for this event");
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
                    ProcessedAt = DateTime.Now,
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
