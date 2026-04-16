using CSCourse.Models;
using System.ComponentModel.DataAnnotations;

namespace CSCourse.Services
{
    /// <summary>
    /// Сервис для работы с мероприятиями (Events). Предоставляет бизнес‑логику для управления событиями:
    /// получение списка с фильтрацией и пагинацией, поиск по ID, создание, обновление и удаление.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Получает пагинированный список всех мероприятий без фильтрации.
        /// </summary>
        /// <param name="page">Номер запрашиваемой страницы (нумерация с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>Объект PaginatedResult, содержащий:
        /// - список мероприятий (items);
        /// - общее количество записей (totalCount);
        /// - номер текущей страницы (page);
        /// - размер страницы (pageSize).</returns>
        PaginatedResult GetAll(int page, int pageSize);

        /// <summary>
        /// Получает пагинированный список мероприятий с применением фильтров.
        /// </summary>
        /// <param name="filterEvent">Объект фильтра с критериями отбора мероприятий.</param>
        /// <param name="page">Номер запрашиваемой страницы (нумерация с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>Объект PaginatedResult с отфильтрованным и пагинированным списком мероприятий.</returns>
        /// <remarks>
        /// Поддерживаемые критерии фильтрации:
        /// - частичное совпадение по названию (без учёта регистра);
        /// - диапазон дат начала (startAt);
        /// - диапазон дат окончания (endAt).
        /// </remarks>
        PaginatedResult GetAll(FilterEvent filterEvent, int page, int pageSize);

        /// <summary>
        /// Получает детальную информацию о мероприятии по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия (целое положительное число).</param>
        /// <returns>Объект Event, если мероприятие найдено; null — если мероприятие с указанным ID отсутствует.</returns>
        /// <remarks>
        /// Возвращает полную информацию о мероприятии, включая все поля модели Event.
        /// </remarks>
        Event? GetEventById(int id);

        /// <summary>
        /// Создаёт новое мероприятие в системе.
        /// </summary>
        /// <param name="event">Объект Event с данными для создания мероприятия.
        /// Поле Id должно быть равно 0 (автоматически генерируется в БД).</param>
        /// <returns>Уникальный идентификатор (ID) созданного мероприятия.</returns>
        /// <exception cref="ValidationException">Выбрасывается при нарушении правил валидации данных.</exception>
        /// <exception cref="ArgumentNullException">Выбрасывается, если переданный объект event равен null.</exception>
        int CreateEvent(Event @event);

        /// <summary>
        /// Полностью обновляет данные существующего мероприятия.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия, которое необходимо обновить.</param>
        /// <param name="event">Объект Event с обновлёнными данными мероприятия.
        /// ID в объекте должен соответствовать параметру id.</param>
        /// <exception cref="InvalidOperationException">Выбрасывается, если мероприятие с указанным ID не найдено.</exception>
        /// <exception cref="ValidationException">Выбрасывается при нарушении правил валидации данных.</exception>
        /// <exception cref="ArgumentNullException">Выбрасывается, если переданный объект event равен null.</exception>
        void UpdateEvent(int id, Event @event);

        /// <summary>
        /// Удаляет мероприятие из системы по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия, подлежащего удалению.</param>
        /// <exception cref="InvalidOperationException">Выбрасывается, если мероприятие с указанным ID не найдено.</exception>
        /// <remarks>
        /// Операция необратима. Все связанные данные (например, регистрации участников) также могут быть удалены
        /// в соответствии с бизнес‑правилами системы.
        /// </remarks>
        void DeleteEvent(int id);
    }
}
