using CSCourse.Domain.Models;
using CSCourse.Application.Models;

namespace CSCourse.Application.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с данными мероприятий: создание, получение, обновление, удаление и фильтрация событий в хранилище.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Создаёт новое мероприятие в хранилище данных.
        /// </summary>
        /// <param name="event">Объект <see cref="Event"/> с данными для создания мероприятия.</param>
        /// <returns>
        /// Уникальный идентификатор (GUID) созданного мероприятия.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Выбрасывается в случае ошибки при сохранении данных в базе данных. При возникновении исключения
        /// выполняется повторная попытка создания мероприятия.
        /// </exception>
        Guid Create(Event @event);

        /// <summary>
        /// Асинхронно создаёт новое мероприятие в хранилище данных.
        /// </summary>
        /// <param name="event">Объект <see cref="Event"/> с данными для создания мероприятия.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает уникальный идентификатор (GUID)
        /// созданного мероприятия после успешного сохранения в базе данных.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Выбрасывается в случае ошибки при асинхронном сохранении данных в базе данных. При возникновении
        /// исключения выполняется повторная асинхронная попытка создания мероприятия.
        /// </exception>
        Task<Guid> CreateAsync(Event @event);

        /// <summary>
        /// Получает информацию о мероприятии по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия, которое необходимо получить.</param>
        /// <returns>
        /// Объект <see cref="Event"/>, соответствующий указанному идентификатору.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается, если мероприятие с указанным идентификатором не найдено.
        /// </exception>
        Event GetById(Guid id);
        // TODO: Может использовать перегрузку методов?
        /// <summary>
        /// Асинхронно получает информацию о мероприятии по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия, которое необходимо получить.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает объект <see cref="Event"/>,
        /// соответствующий указанному идентификатору.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается, если мероприятие с указанным идентификатором не найдено.
        /// </exception>
        Task<Event> GetByIdAsync(Guid id);

        /// <summary>
        /// Получает отфильтрованную постраничную коллекцию мероприятий.
        /// </summary>
        /// <param name="filterEvent">Объект <see cref="FilterRepositoryEventDto"/> с параметрами фильтрации.</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>
        /// Коллекция объектов <see cref="Event"/>, отфильтрованных и разбитых на страницы.
        /// Фильтрация выполняется по заголовку (частичное совпадение), дате начала и дате окончания.
        /// </returns>
        List<Event> GetFilteredPage(FilterRepositoryEventDto filterEvent, int page, int pageSize);

        /// <summary>
        /// Асинхронно получает отфильтрованную постраничную коллекцию мероприятий.
        /// </summary>
        /// <param name="filterEvent">Объект <see cref="FilterRepositoryEventDto"/> с параметрами фильтрации.</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает коллекцию объектов <see cref="Event"/>,
        /// отфильтрованных и разбитых на страницы.
        /// </returns>
        Task<List<Event>> GetFilteredPageAsync(FilterRepositoryEventDto filterEvent, int page, int pageSize);

        /// <summary>
        /// Получает постраничную коллекцию всех мероприятий.
        /// </summary>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>
        /// Коллекция объектов <see cref="Event"/>, разбитая на страницы.
        /// </returns>
        List<Event> GetPage(int page, int pageSize);

        /// <summary>
        /// Асинхронно получает постраничную коллекцию всех мероприятий.
        /// </summary>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает коллекцию объектов <see cref="Event"/>,
        /// разбитую на страницы.
        /// </returns>
        Task<List<Event>> GetPageAsync(int page, int pageSize);

        /// <summary>
        /// Проверяет существование мероприятия по его идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <returns>
        /// Значение <c>true</c>, если мероприятие существует; иначе — <c>false</c>.
        /// </returns>
        bool IsExists(Guid id);

        /// <summary>
        /// Асинхронно проверяет существование мероприятия по его идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>,
        /// если мероприятие существует; иначе — <c>false</c>.
        /// </returns>
        Task<bool> IsExistsAsync(Guid id);

        /// <summary>
        /// Пытается зарезервировать указанное количество мест для мероприятия.
        /// Операция выполняется в транзакции с уровнем изоляции RepeatableRead.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <param name="count">Количество мест, которые необходимо зарезервировать.</param>
        /// <returns>
        /// Значение <c>true</c>, если места успешно зарезервированы; иначе — <c>false</c>
        /// (если доступных мест недостаточно).
        /// </returns>
        bool TryReserveSeats(Guid id, int count);

        /// <summary>
        /// Асинхронно пытается зарезервировать указанное количество мест для мероприятия.
        /// Операция выполняется в транзакции с уровнем изоляции RepeatableRead.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <param name="count">Количество мест, которые необходимо зарезервировать.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>,
        /// если места успешно зарезервированы; иначе — <c>false</c>.
        /// </returns>
        Task<bool> TryReserveSeatsAsync(Guid id, int count);

        /// <summary>
        /// Пытается освободить указанное количество зарезервированных мест для мероприятия.
        /// Операция выполняется в транзакции с уровнем изоляции RepeatableRead.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <param name="count">Количество мест, которые необходимо освободить.</param>
        /// <returns>
        /// Значение <c>true</c>, если места успешно освобождены; иначе — <c>false</c>
        /// (если общее количество мест превысит максимально допустимое).
        /// </returns>
        bool TryReleaseSeats(Guid id, int count);

        /// <summary>
        /// Асинхронно пытается освободить указанное количество зарезервированных мест для мероприятия.
        /// Операция выполняется в транзакции с уровнем изоляции RepeatableRead.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия.</param>
        /// <param name="count">Количество мест, которые необходимо освободить.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>,
        /// если места успешно освобождены; иначе — <c>false</c>.
        /// </returns>
        Task<bool> TryReleaseSeatsAsync(Guid id, int count);

        /// <summary>
        /// Обновляет данные мероприятия в хранилище.
        /// Операция выполняется в транзакции с уровнем изоляции RepeatableRead.
        /// </summary>
        /// <param name="event">Объект <see cref="EventRepositoryUpdateDto"/> с обновлёнными данными мероприятия.</param>
        /// <returns>
        /// Значение <c>true</c>, если мероприятие успешно обновлено; иначе — <c>false</c>.
        /// </returns>
        bool Update(EventRepositoryUpdateDto @event);

        /// <summary>
        /// Асинхронно обновляет данные мероприятия в хранилище.
        /// </summary>
        /// <param name="event">Объект <see cref="EventRepositoryUpdateDto"/> с обновлёнными данными мероприятия.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>,
        /// если мероприятие было успешно обновлено (затронута хотя бы одна строка в БД);
        /// в противном случае — <c>false</c>.
        /// </returns>
        Task<bool> UpdateAsync(EventRepositoryUpdateDto @event);

        /// <summary>
        /// Удаляет мероприятие из хранилища по его идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия, которое необходимо удалить.</param>
        /// <returns>
        /// Значение <c>true</c>, если мероприятие успешно удалено; иначе — <c>false</c>
        /// (если мероприятие не найдено).
        /// </returns>
        bool Delete(Guid id);

        /// <summary>
        /// Асинхронно удаляет мероприятие из хранилища по его идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) мероприятия, которое необходимо удалить.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>,
        /// если мероприятие успешно удалено; иначе — <c>false</c>.
        /// </returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Получает общее количество мероприятий в хранилище.
        /// </summary>
        /// <returns>
        /// Целое число, представляющее общее количество мероприятий.
        /// </returns>
        int Count();

        /// <summary>
        /// Асинхронно получает общее количество мероприятий в хранилище.
        /// </summary>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает целое число —
        /// общее количество мероприятий.
        /// </returns>
        Task<int> CountAsync();
    }
}
