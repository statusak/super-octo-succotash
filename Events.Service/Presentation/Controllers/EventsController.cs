using Events.Service.Domain.Models;
using Events.Service.Application.Interfaces;
using Events.Service.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Identity.Service.Controllers
{
    /// <summary>
    /// Контроллер для управления мероприятиями (Events). Предоставляет REST API endpoints для получения, создания, обновления, удаления мероприятий,
    /// а также для работы с бронированиями (создание бронирований для мероприятий).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера <see cref="EventsController"/>.
        /// </summary>
        /// <param name="eventService">Сервис для работы с мероприятиями.</param>
        /// <param name="logger">Логгер для записи событий контроллера.</param>
        public EventsController(
            IEventService eventService,
            ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }
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
        /// GET /Events?page=2&amp;pageSize=5&amp;title=конференция
        ///
        /// Пример ответа (HTTP 200 OK):
        /// <code>
        /// {
        ///   "events": [
        ///     {
        ///       "id": "308dd020-a855-4e80-b29e-b3582b6de65c",
        ///       "title": "Конференция разработчиков",
        ///       "description": "Ежегодная конференция...",
        ///       "totalSeats": 10,
        ///       "availableSeats": 10,
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
        public async Task<ActionResult<PaginatedResult>> GetAll([FromQuery] FilterEventDto? filterEventDto, int? page, int? pageSize)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var filterEvent = new FilterEvent
            {
                Title = string.IsNullOrEmpty(filterEventDto?.Title) ? "" : filterEventDto.Title,
                StartAt = filterEventDto?.StartAt,
                EndAt = filterEventDto?.EndAt,
            };

            // TODO: Возвращать EventInfoDto, т.к. выводятся все Booking 

            return Ok(await _eventService.GetAllAsync(filterEvent, page ?? 1, pageSize ?? 10));
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
        /// GET /Events/308dd020-a855-4e80-b29e-b3582b6de65c
        /// </remarks>
        /// <response code="200">Успешный ответ: информация о мероприятии (HTTP 200 OK)</response>
        /// <response code="404">Мероприятие с указанным ID не найдено (HTTP 404 Not Found)</response>
        [HttpGet("{index:guid}")]
        public async Task<ActionResult<Event>> GetById(Guid index)
        {
            try
            {
                var eventItem = await _eventService.GetEventByIdCacheAsync(index);
                // TODO: Возвращать EventInfoDto, т.к. выводится поле Booking 
                return Ok(eventItem);
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        [HttpGet("top")]
        public async Task<ActionResult<List<Event>>> GetTop10()
        {
            var eventItems = await _eventService.GetTop10Async();
            // TODO: Возвращать EventInfoDto, т.к. выводится поле Booking 
            return Ok(eventItems);
        }

        /// <summary>
        /// Создаёт новое мероприятие в системе.
        /// </summary>
        /// <param name="eventDto">Модель данных для создания мероприятия (обязательный параметр, в формате JSON).
        /// Должна содержать обязательные поля: <c>Title</c>, <c>StartAt</c>, <c>EndAt</c>. Поле <c>TotalSeats</c>
        /// определяет общее количество мест на мероприятии, а <c>AvailableSeats</c> автоматически инициализируется равным <c>TotalSeats</c>.</param>
        /// <remarks>
        /// Добавляет новое мероприятие на основе переданных данных.
        /// Для успешного создания требуется валидная модель <see cref="EventCreateDto"/> с заполненными обязательными полями.
        /// При создании автоматически инициализирует начальное количество доступных мест равным общему количеству мест.
        ///
        /// Пример тела запроса (JSON):
        /// <code>
        /// {
        ///   "title": "Новая конференция",
        ///   "description": "Описание мероприятия",
        ///   "totalSeats": 100,
        ///   "startAt": "2024-01-15T09:00:00",
        ///   "endAt": "2024-01-15T17:00:00"
        /// }
        /// </code>
        /// </remarks>
        /// <returns>HTTP статус 202 Accepted с объектом мероприятия и заголовком Location, указывающим на URL созданного ресурса.</returns>
        /// <response code="202">Мероприятие успешно создано. Возвращается объект мероприятия и ссылка на ресурс (Location header).</response>
        /// <response code="400">Ошибка валидации или некорректные данные (HTTP 400 Bad Request)</response>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(Event))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<ActionResult<Event>> Post([FromBody] EventCreateDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = Guid.Empty,
                Title = eventDto.Title,
                TotalSeats = eventDto.TotalSeats,
                AvailableSeats = eventDto.TotalSeats,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt,
            };

            @event.Id = await _eventService.CreateEventAsync(@event);
            // TODO: Возвращать EventInfoDto, т.к. выводится поле Booking 

            return CreatedAtAction(
                actionName: nameof(GetById),
                routeValues: new { index = @event.Id },
                value: @event
            );
        }

        /// <summary>
        /// Полностью обновляет существующее мероприятие.
        /// </summary>
        /// <param name="index">Идентификатор мероприятия, которое необходимо обновить (в формате GUID).</param>
        /// <param name="eventDto">Обновлённые данные мероприятия (в формате JSON). Должны содержать поля:
        /// <c>Title</c>, <c>Description</c> (может быть <c>null</c>), <c>StartAt</c>, <c>EndAt</c>.
        /// Обратите внимание: поле <c>TotalSeats</c> не может быть изменено через этот метод — для работы с местами используйте специализированные операции.</param>
        /// <remarks>
        /// Заменяет все данные существующего мероприятия на новые, кроме идентификатора и параметров, связанных с местами.
        /// Требует валидной модели <see cref="EventUpdateDto"/> с заполненными полями.
        /// Если мероприятие с указанным ID не существует, возвращается ошибка 404.
        ///
        /// Пример запроса:
        /// PUT /Events/308dd020-a855-4e80-b29e-b3582b6de65c
        /// С телом запроса (JSON):
        /// <code>
        /// {
        ///   "title": "Обновлённое название",
        ///   "description": "Новое описание",
        ///   "startAt": "2024-02-15T10:00:00",
        ///   "endAt": "2024-02-15T18:00:00"
        /// }
        /// </code>
        /// </remarks>
        /// <returns>HTTP статус 204 No Content при успешном обновлении.</returns>
        /// <response code="204">Данные мероприятия успешно обновлены (HTTP 204 No Content)</response>
        /// <response code="400">Некорректные данные или ошибки валидации (HTTP 400 Bad Request)</response>
        /// <response code="404">Мероприятие не найдено (HTTP 404 Not Found)</response>
        [Authorize(Roles = "Admin")]
        [HttpPut("{index:guid}")]
        public async Task<ActionResult> Put(Guid index, [FromBody] EventUpdateDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                bool res = await _eventService.UpdateEventAsync(index, eventDto.Title, eventDto.Description, eventDto.StartAt, eventDto.EndAt);
                if (res)
                {
                    return NoContent();
                } 
                return NotFound($"Event with index {index} not found");
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
        /// DELETE /Events/308dd020-a855-4e80-b29e-b3582b6de65c
        /// </remarks>
        /// <response code="200">Мероприятие успешно удалено (HTTP 200 OK)</response>
        /// <response code="404">Мероприятие не найдено в системе (HTTP 404 Not Found)</response>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{index:guid}")]
        public async Task<ActionResult> Delete(Guid index)
        {
            try
            {
                if(await _eventService.DeleteEventAsync(index))
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
