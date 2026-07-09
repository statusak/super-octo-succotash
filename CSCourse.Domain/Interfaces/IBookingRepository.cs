using CSCourse.Domain.Models;

namespace CSCourse.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с данными бронирований: создание, получение и обновление записей о бронированиях в хранилище.
    /// </summary>
    public interface IBookingRepository
    {
        /// <summary>
        /// Создаёт новое бронирование в хранилище данных.
        /// </summary>
        /// <param name="booking">Объект <see cref="BookingRepositoryCreateDto"/> с данными для создания бронирования.</param>
        /// <returns>
        /// Уникальный идентификатор (GUID) созданного бронирования.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Выбрасывается в случае ошибки при сохранении данных в базе данных. При возникновении исключения
        /// выполняется повторная попытка создания бронирования.
        /// </exception>
        Guid Create(BookingRepositoryCreateDto booking);

        /// <summary>
        /// Получает коллекцию всех бронирований со статусом «ожидает обработки» (Pending).
        /// </summary>
        /// <returns>
        /// Коллекция объектов <see cref="Booking"/>, у которых свойство <see cref="Booking.Status"/>
        /// имеет значение <see cref="BookingStatus.Pending"/>.
        /// </returns>
        /// <remarks>
        /// Метод позволяет получить список всех незавершённых бронирований, которые ещё не были обработаны системой.
        /// </remarks>
        IEnumerable<Booking> GetPending();

        /// <summary>
        /// Асинхронно создаёт новое бронирование в хранилище данных.
        /// </summary>
        /// <param name="booking">Объект <see cref="BookingRepositoryCreateDto"/> с данными для создания бронирования.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает уникальный идентификатор (GUID)
        /// созданного бронирования после успешного сохранения в базе данных.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Выбрасывается в случае ошибки при асинхронном сохранении данных в базе данных. При возникновении
        /// исключения выполняется повторная асинхронная попытка создания бронирования.
        /// </exception>
        Task<Guid> CreateAsync(BookingRepositoryCreateDto booking);

        /// <summary>
        /// Асинхронно получает коллекцию всех бронирований со статусом «ожидает обработки» (Pending).
        /// </summary>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает коллекцию объектов <see cref="Booking"/>,
        /// у которых свойство <see cref="Booking.Status"/> имеет значение <see cref="BookingStatus.Pending"/>.
        /// </returns>
        /// <remarks>
        /// Метод позволяет асинхронно получить список всех незавершённых бронирований,
        /// которые ещё не были обработаны системой.
        /// </remarks>
        Task<IEnumerable<Booking>> GetPendingAsync();

        /// <summary>
        /// Асинхронно получает информацию о бронировании по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор (GUID) бронирования, которое необходимо получить.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает объект <see cref="Booking"/>,
        /// если бронирование найдено; в противном случае возвращает <c>null</c>.
        /// </returns>
        Task<Booking?> GetByIdAsync(Guid id);

        /// <summary>
        /// Асинхронно обновляет данные бронирования в хранилище.
        /// </summary>
        /// <param name="booking">Объект <see cref="BookingRepositoryUpdateDto"/> с обновлёнными данными бронирования.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает <c>true</c>, если бронирование
        /// было успешно обновлено (затронута хотя бы одна строка в БД); в противном случае — <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Обновляются только свойства <see cref="Booking.Status"/> и <see cref="Booking.ProcessedAt"/>.
        /// </remarks>
        Task<bool> UpdateAsync(BookingRepositoryUpdateDto booking);
    }
}