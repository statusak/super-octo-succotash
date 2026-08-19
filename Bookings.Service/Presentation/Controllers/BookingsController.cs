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
            if (role == null)
                return BadRequest("User role not found");

            try
            {
                // Инициируем бронирование: сохраняем в БД (Pending) + отправляем событие в Kafka
                var booking = await _bookingService.InitiateBookingAsync(eventId, userId.Value);

                _logger.LogInformation("Booking initiated. Id={BookingId}, EventId={EventId}", booking.Id, eventId);

                var response = new BookingResponseDto
                {
                    Id = booking.Id,
                    EventId = booking.EventId,
                    CreatedAt = booking.CreatedAt,
                    Status = booking.Status,
                    ProcessedAt = booking.ProcessedAt
                };

                return AcceptedAtAction(
                    actionName: nameof(GetById),
                    controllerName: nameof(BookingsController).Replace("Controller", ""),
                    routeValues: new { id = booking.Id },
                    value: response
                );
            }
            catch (UnauthorizedOperationException ex)
            {
                return Forbid(ex.Message);
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
        /// <param name="id">Уникальный идентификатор (GUID) бронирования, которое необходимо получить.</param>
        /// <returns>
        /// Возвращает <see cref="ActionResult"/> с данными бронирования, если запись найдена;
        /// в противном случае возвращает ответ 404 Not Found с текстовым сообщением об ошибке.
        /// </returns>
        /// <response code="200">Успешно получен объект бронирования.</response>
        /// <response code="404">Бронирование с указанным идентификатором не найдено.</response>
        [HttpGet("{id:Guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Booking))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User ID not found in claims");

            var role = GetCurrentUserRole();
            if (role == null)
                return BadRequest("User role not found");

            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id, userId.Value, role.Value);
                if (booking == null)
                    return NotFound($"Booking with id {id} not found");

                var response = new BookingResponseDto
                {
                    Id = booking.Id,
                    EventId = booking.EventId,
                    CreatedAt = booking.CreatedAt,
                    ProcessedAt = booking.ProcessedAt,
                    Status = booking.Status
                };

                return Ok(response);
            }
            catch (UnauthorizedOperationException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting booking by id");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Cancel(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User ID not found in claims");

            var role = GetCurrentUserRole();
            if(role == null)
                return BadRequest("User role not found");

            try
            {
                await _bookingService.CancelBookingAsync(id, userId.Value, role.Value);
                _logger.LogInformation("Cancel request initiated for booking {BookingId}", id);

                return Accepted(new
                {
                    id,
                    status = "Cancelling",
                    message = "Cancel request sent to queue."
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedOperationException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while cancelling booking");
                return StatusCode(500, "Internal server error");
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
