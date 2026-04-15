using CSCourse.Models;

namespace CSCourse.Services
{
    /// <summary>
    /// Сервис для работы с мероприятиями: предоставляет методы для получения, создания, обновления и удаления событий.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Получает список всех мероприятий.
        /// </summary>
        /// <returns>Список объектов Event.</returns>
        List<Event> GetAll(FilterEvent @filterEvent);

        /// <summary>
        /// Получает мероприятие по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия.</param>
        /// <returns>Объект Event, если мероприятие найдено; null, если мероприятие не существует.</returns>
        Event? GetEventById(int id);

        /// <summary>
        /// Создаёт новое мероприятие.
        /// </summary>
        /// <param name="event">Объект Event с данными для создания мероприятия.</param>
        void CreateEvent(Event @event);

        /// <summary>
        /// Обновляет существующее мероприятие по указанному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия, которое нужно обновить.</param>
        /// <param name="event">Объект Event с обновлёнными данными мероприятия.</param>
        void UpdateEvent(int id, Event @event);

        /// <summary>
        /// Удаляет мероприятие по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор мероприятия, которое нужно удалить.</param>
        void DeleteEvent(int id);
    }

}
