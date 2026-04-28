using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{
    /// <summary>
    /// Контроллер для управления мероприятиями (Events). Предоставляет REST API endpoints для получения, создания, обновления и удаления мероприятий.
    /// </summary>
    [ApiController]
    [Route("/[controller]")]
    public class EventsController : ControllerBase
    {

        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        private readonly IBookingTaskQueue _bookingTaskQueue;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IEventService eventService,
            IBookingService bookingService,
            IBookingTaskQueue bookingTaskQueue,
            ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _bookingService = bookingService;
            _bookingTaskQueue = bookingTaskQueue;
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
                Title = string.IsNullOrEmpty(filterEventDto?.Title) ? "" : filterEventDto.Title,
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
        [HttpGet("{index:guid}")]
        public ActionResult<Event> GetById(Guid index)
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
        /// <response code="202">Мероприятие успешно создано. Возвращается объект мероприятия и ссылка на ресурс (Location header).</response>
        /// <response code="400">Ошибка валидации или некорректные данные (HTTP 400 Bad Request)</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(Event))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public ActionResult<Event> Post([FromBody] EventDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = Guid.Empty,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt,
            };

            @event.Id = _eventService.CreateEvent(@event);

            return CreatedAtAction(
                actionName: nameof(GetById),
                routeValues: new { index = @event.Id },
                value: @event
            );
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
        [HttpPut("{index:guid}")]
        public ActionResult Put(Guid index, [FromBody] EventDto eventDto)
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
        [HttpDelete("{index:guid}")]
        public ActionResult Delete(Guid index)
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


        /// <summary>
        /// Создаёт новое бронирование для указанного мероприятия.
        /// </summary>
        /// <param name="eventId">Уникальный идентификатор (GUID) мероприятия, для которого создаётся бронирование.</param>
        /// <returns>
        /// Возвращает <see cref="ActionResult"/> со статусом 202 Accepted и данными созданного бронирования,
        /// включая URL для получения информации о бронировании;
        /// в случае ошибки возвращает ответ 404 Not Found с текстовым сообщением.
        /// </returns>
        /// <response code="202">Бронирование успешно создано. Возвращается объект бронирования и ссылка на ресурс (Location header).</response>
        /// <response code="404">Мероприятие с указанным идентификатором не найдено.</response>
        [HttpPost("{eventId:Guid}/book")]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(Booking))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult> CreateBooking(Guid eventId)
        {
            try
            {
                _eventService.GetEventById(eventId);
                var created = await _bookingService.CreateBookingAsync(eventId);
                _bookingTaskQueue.Enqueue(created);

                BookingResponseDto response =
                new BookingResponseDto
                {
                    Id = created.Id,
                    EventId = created.EventId,
                    CreatedAt = created.CreatedAt,
                    Status = created.Status,
                };

                return AcceptedAtAction(
                    actionName: nameof(BookingsController.GetById),
                    controllerName: "Bookings", // TODO: Убрать хардкодинг
                    routeValues: new { index = created.Id },
                    value: response
                );
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {eventId} not found");
            }
        }
    }
}
