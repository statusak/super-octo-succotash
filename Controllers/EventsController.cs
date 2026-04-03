using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Net;

namespace CSCourse.Controllers
{
    /// <summary>
    /// Контроллер для управления мероприятиями (Events). Предоставляет REST API endpoints для получения, создания, обновления и удаления мероприятий.
    /// </summary>
    [ApiController]
    [Route("/[controller]")]
    public class EventsController(IEventService _eventService) : ControllerBase
    {
        /// <summary>
        /// Получает список всех мероприятий.
        /// </summary>
        /// <remarks>
        /// Возвращает полный список доступных мероприятий в системе.
        /// Пример ответа:
        /// [
        ///   {
        ///     "id": 1,
        ///     "title": "Конференция разработчиков",
        ///     "description": "Ежегодная конференция...",
        ///     "startAt": "2023-12-01T10:00:00",
        ///     "endAt": "2023-12-01T18:00:00"
        ///   }
        /// ]
        /// </remarks>
        /// <returns>Список мероприятий (HTTP 200 OK)</returns>
        [HttpGet]
        public ActionResult<List<Event>> GetAll()
        {
            return Ok(_eventService.GetAll());
        }

        /// <summary>
        /// Получает мероприятие по его идентификатору.
        /// </summary>
        /// <param name="index">Уникальный идентификатор мероприятия (целое число)</param>
        /// <remarks>
        /// Поиск мероприятия по ID. Если мероприятие не найдено, возвращается ошибка 404.
        /// </remarks>
        /// <response code="200">Мероприятие успешно найдено</response>
        /// <response code="404">Мероприятие не найдено</response>
        [HttpGet("{index:int}")]
        public ActionResult<Event> GetById(int index)
        {
            try
            {
                return Ok(_eventService.GetEventById(index));
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        /// <summary>
        /// Создаёт новое мероприятие.
        /// </summary>
        /// <param name="eventDto">Данные мероприятия для создания (в формате JSON)</param>
        /// <remarks>
        /// Создаёт новое мероприятие на основе переданных данных.
        /// Требуется валидная модель EventDto.
        /// </remarks>
        /// <response code="201">Мероприятие успешно создано</response>
        /// <response code="400">Некорректные данные или ошибки валидации</response>
        [HttpPost]
        public ActionResult Post([FromBody] EventDto @eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = 0,
                Title = @eventDto.Title,
                Description = @eventDto.Description,
                StartAt = @eventDto.StartAt,
                EndAt = @eventDto.EndAt,
            };

            _eventService.CreateEvent(@event);
            return Created();
        }

        /// <summary>
        /// Обновляет существующее мероприятие.
        /// </summary>
        /// <param name="index">Идентификатор мероприятия для обновления</param>
        /// <param name="eventDto">Обновлённые данные мероприятия</param>
        /// <remarks>
        /// Полностью заменяет данные существующего мероприятия.
        /// Если мероприятие с указанным ID не найдено, возвращается ошибка 404.
        /// </remarks>
        /// <response code="204">Мероприятие успешно обновлено</response>
        /// <response code="400">Некорректные данные или ошибки валидации</response>
        /// <response code="404">Мероприятие не найдено</response>
        [HttpPut("{index:int}")]
        public ActionResult Put(int index, [FromBody] EventDto @eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = 0,
                Title = @eventDto.Title,
                Description = @eventDto.Description,
                StartAt = @eventDto.StartAt,
                EndAt = @eventDto.EndAt,
            };

            try
            {
                _eventService.UpdateEvent(index, @event);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        /// <summary>
        /// Удаляет мероприятие по идентификатору.
        /// </summary>
        /// <param name="index">Идентификатор удаляемого мероприятия</param>
        /// <remarks>
        /// Удаляет мероприятие из системы.
        /// Если мероприятие не найдено, возвращается ошибка 404.
        /// </remarks>
        /// <response code="200">Мероприятие успешно удалено</response>
        /// <response code="404">Мероприятие не найдено</response>
        [HttpDelete("{index:int}")]
        public ActionResult Delete(int index)
        {
            try
            {
                _eventService.DeleteEvent(index);
                return Ok();
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }
    }
}
