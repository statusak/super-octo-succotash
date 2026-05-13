using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{

    /// <summary>
    /// Контроллер для работы с бронированиями.
    /// </summary>
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
            Booking? booking = await _bookingService.GetBookingByIdAsync(index);
            if (booking != null)
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
    }
}
