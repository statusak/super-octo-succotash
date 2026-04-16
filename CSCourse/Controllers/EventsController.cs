using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;

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
        /// Получает список мероприятий с возможностью фильтрации и пагинации.
        /// </summary>
        /// <param name="filterEventDto">Параметры фильтрации мероприятий (опционально)</param>
        /// <param name="page">Номер страницы для возврата (опционально, по умолчанию — 1, первая страница)</param>
        /// <param name="pageSize">Количество элементов на странице (опционально, по умолчанию — 10)</param>
        /// <remarks>
        /// Возвращает пагинированный список мероприятий, соответствующих заданным критериям фильтрации.
        /// Поддерживает фильтрацию по названию (частичное совпадение), датам начала и окончания.
        ///
        /// Пример запроса:
        /// GET /Events?page=2&pageSize=5&title=конференция
        ///
        /// Пример ответа (HTTP 200 OK):
        /// <code>
        /// {
        ///   "events": [
        ///     {
        ///       "id": 1,
        ///       "title": "Конференция разработчиков",
        ///       "description": "Ежегодная конференция...",
        ///       "startAt": "2023-12-01T10:00:00",
        ///       "endAt": "2023-12-01T18:00:00"
        ///     }
        ///   ],
        ///   "countEvents": 25,
        /// }
        /// </code>
        /// </remarks>
        /// <returns>Пагинированный результат с списком мероприятий</returns>
        [HttpGet]
        public ActionResult<PaginatedResult> GetAll([FromQuery] FilterEventDto? filterEventDto, int? page, int? pageSize)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var filterEvent = new FilterEvent
            {
                Title = string.IsNullOrEmpty(filterEventDto?.Title) ? "" : filterEventDto.Title.ToLower(),
                StartAt = filterEventDto?.StartAt,
                EndAt = filterEventDto?.EndAt,
            };

            return Ok(_eventService.GetAll(filterEvent, page ?? 1, pageSize ?? 10));
        }

        /// <summary>
        /// Получает детальную информацию о конкретном мероприятии по его идентификатору.
        /// </summary>
        /// <param name="index">Уникальный идентификатор мероприятия (целое положительное число)</param>
        /// <remarks>
        /// Выполняет поиск мероприятия в системе по указанному ID.
        /// Если мероприятие найдено, возвращает полную информацию о нём.
        /// В случае отсутствия мероприятия с указанным ID возвращается ошибка 404 (Not Found).
        ///
        /// Пример запроса:
        /// GET /Events/1
        /// </remarks>
        /// <response code="200">Успешный ответ: информация о мероприятии (HTTP 200 OK)</response>
        /// <response code="404">Мероприятие с указанным ID не найдено (HTTP 404 Not Found)</response>
        [HttpGet("{index:int}")]
        public ActionResult<Event> GetById(int index)
        {
            try
            {
                var eventItem = _eventService.GetEventById(index);
                return Ok(eventItem);
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        /// <summary>
        /// Создаёт новое мероприятие в системе.
        /// </summary>
        /// <param name="eventDto">Модель данных для создания мероприятия (обязательный параметр, в формате JSON)</param>
        /// <remarks>
        /// Добавляет новое мероприятие на основе переданных данных.
        /// Для успешного создания требуется валидная модель EventDto с заполненными обязательными полями.
        ///
        /// Пример тела запроса (JSON):
        /// <code>
        /// {
        ///   "title": "Новая конференция",
        ///   "description": "Описание мероприятия",
        ///   "startAt": "2024-01-15T09:00:00",
        ///   "endAt": "2024-01-15T17:00:00"
        /// }
        /// </code>
        /// </remarks>
        /// <response code="201">Мероприятие успешно создано (HTTP 201 Created)</response>
        /// <response code="400">Ошибка валидации или некорректные данные (HTTP 400 Bad Request)</response>
        [HttpPost]
        public ActionResult Post([FromBody] EventDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = 0,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt,
            };

            _eventService.CreateEvent(@event);
            return Created();
        }

        /// <summary>
        /// Полностью обновляет существующее мероприятие.
        /// </summary>
        /// <param name="index">Идентификатор мероприятия, которое необходимо обновить</param>
        /// <param name="eventDto">Обновлённые данные мероприятия (в формате JSON)</param>
        /// <remarks>
        /// Заменяет все данные существующего мероприятия на новые.
        /// Требует валидной модели EventDto с заполненными полями.
        /// Если мероприятие с указанным ID не существует, возвращается ошибка 404.
        ///
        /// Пример запроса:
        /// PUT /Events/1
        /// С телом запроса (JSON), аналогичным методу POST.
        /// </remarks>
        /// <response code="204">Данные мероприятия успешно обновлены (HTTP 204 No Content)</response>
        /// <response code="400">Некорректные данные или ошибки валидации (HTTP 400 Bad Request)</response>
        /// <response code="404">Мероприятие не найдено (HTTP 404 Not Found)</response>
        [HttpPut("{index:int}")]
        public ActionResult Put(int index, [FromBody] EventDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = index,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt,
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
        /// Удаляет мероприятие из системы по его идентификатору.
        /// </summary>
        /// <param name="index">Идентификатор мероприятия, подлежащего удалению</param>
        /// <remarks>
        /// Производит удаление мероприятия из базы данных.
        /// Операция необратима.
        /// Если мероприятие не существует, возвращается ошибка 404.
        ///
        /// Пример запроса:
        /// DELETE /Events/1
        /// </remarks>
        /// <response code="200">Мероприятие успешно удалено (HTTP 200 OK)</response>
        /// <response code="404">Мероприятие не найдено в системе (HTTP 404 Not Found)</response>
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
