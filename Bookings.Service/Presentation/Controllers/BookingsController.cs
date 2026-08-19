using Bookings.Service.Domain.Models;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CSCourse.Contracts.Models;
using CSCourse.Contracts.Exceptions;

namespace Bookings.Service.Controllers
{

    /// <summary>
    /// Контроллер для работы с бронированиями.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(
            IBookingService bookingService,
            ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        /// <summary>
        /// Инициирует бронирование мероприятия.
        /// </summary>
        /// <param name="eventId">Идентификатор мероприятия, на которое запрашивается бронирование.</param>
        /// <remarks>
        /// Метод НЕ проверяет существование мероприятия заранее. Вместо этого:
        /// 1. Создаётся запись бронирования со статусом Pending.
        /// 2. В Kafka публикуется событие BookingCreated.
        /// 3. Ответ клиенту — 202 Accepted.
        ///
        /// Финальное решение (подтверждение или отказ) принимается Event.Service асинхронно
        /// и фиксируется в БД через BookingBackgroundService.
        /// </remarks>
        [HttpPost("{eventId:Guid}/book")]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(BookingResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid eventId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User ID not found in claims");

            var role = GetCurrentUserRole();
            if(role == null)
                return BadRequest("User role not found");

            try
            {
                Booking newBooking = await _bookingService.CreateBookingAsync(eventId, userId.Value);

                _logger.LogInformation(
                    "Booking initiated. Id={BookingId}, EventId={EventId}, UserId={UserId}",
                    newBooking.Id, eventId, userId);

                var response = new BookingResponseDto
                {
                    Id = newBooking.Id,
                    EventId = eventId,
                    CreatedAt = newBooking.CreatedAt,
                    Status = newBooking.Status
                };

                return AcceptedAtAction(
                    actionName: nameof(GetById),
                    controllerName: nameof(BookingsController).Replace("Controller", ""),
                    routeValues: new { id = newBooking.Id },
                    value: response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating booking");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает информацию о бронировании по его уникальному идентификатору.
        /// </summary>
        /// <param name="index">Уникальный идентификатор (GUID) бронирования, которое необходимо получить.</param>
        /// <returns>
        /// Возвращает <see cref="ActionResult"/> с данными бронирования, если запись найдена;
        /// в противном случае возвращает ответ 404 Not Found с текстовым сообщением об ошибке.
        /// </returns>
        /// <response code="200">Успешно получен объект бронирования.</response>
        /// <response code="404">Бронирование с указанным идентификатором не найдено.</response>
        [HttpGet("{index:Guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Booking))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult> GetById(Guid index)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User ID not found in claims");

            var role = GetCurrentUserRole();
            if(role == null)
                return BadRequest("User role not found");
            
            Booking? booking = await _bookingService.GetBookingByIdAsync(index);
            if (booking != null && booking.UserId == userId)
            {
                BookingResponseDto response =
                new BookingResponseDto{
                    Id = booking.Id,
                    EventId = booking.EventId,
                    CreatedAt = booking.CreatedAt,
                    ProcessedAt = booking.ProcessedAt,
                    Status = booking.Status,
                };
                return Ok(response);
            }
            return NotFound($"Booking with index {index} not found");
        }

        [HttpDelete("{index:guid}")]
        public async Task<ActionResult> Delete(Guid index)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User ID not found in claims");

            var role = GetCurrentUserRole();
            if(role == null)
                return BadRequest("User role not found");

            try
            {
                if(await _bookingService.CancelledBookingByIdAsync(index, userId.Value, role.Value))
                {
                    return Ok();
                }
                else
                {
                    return NotFound($"Booking with index {index} not found");
                }
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Booking with index {index} not found");
            }
            catch (UnauthorizedOperationException ex)
            {
                return Forbid(ex.Message); 
            }

        }

        #region Helpers

        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                return null;
            return userId;
        }

        private AccountRole? GetCurrentUserRole()
        {
            var claim = User.FindFirst(ClaimTypes.Role);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
                return null;

            if (!Enum.TryParse<AccountRole>(claim.Value, ignoreCase: true, out var role))
                return null;

            return role;
        }

        #endregion
    }
}
