using CSCourse.Domain.Models;
using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CSCourse.Controllers
{

    /// <summary>
    /// Контроллер для работы с бронированиями.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/[controller]")]
    public class BookingsController(IBookingService _bookingService) : ControllerBase
    {
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return BadRequest("ID user not find");
            }

            string userIdString = userIdClaim.Value;
            Guid userId;

            if (!Guid.TryParse(userIdString, out userId))
            {
                return BadRequest($"Bad ID user: {userIdString}");
            }
            
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return BadRequest("ID user not find");
            }

            string userIdString = userIdClaim.Value;
            Guid userId;

            if (!Guid.TryParse(userIdString, out userId))
            {
                return BadRequest($"Bad ID user: {userIdString}");
            }


            var userAccountRoleClaim = User.FindFirst(ClaimTypes.Role);

            if (userAccountRoleClaim == null || string.IsNullOrWhiteSpace(userAccountRoleClaim.Value))
            {
                return BadRequest("User role not found");
            }

            if (!Enum.TryParse<AccountRole>(userAccountRoleClaim.Value, ignoreCase: true, out var role))
            {
                return BadRequest($"Invalid role value: {userAccountRoleClaim.Value}");
            }


            try
            {
                if(await _bookingService.CancelledBookingByIdAsync(index, userId, role))
                {
                    return Ok();
                }
                else
                {
                    return NotFound($"Event with index {index} not found");
                }
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }
    }
}
