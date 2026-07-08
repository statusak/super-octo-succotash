using CSCourse.Models;

namespace CSCourse.Interfaces
{
    /// <summary>
    /// Сервис для работы с бронированиями: создание, получение и обновление обработанных бронирований.
    /// </summary>
    public interface IBookingService
    {
        /// <summary>
        /// Создаёт новое бронирование для указанного мероприятия.
        /// </summary>
        /// <param name="eventId">Уникальный идентификатор (GUID) мероприятия, для которого создаётся бронирование.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает объект <see cref="Booking"/>
        /// после успешного создания бронирования.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается, если мероприятие с указанным идентификатором не найдено.
        /// </exception>
        Task<Booking> CreateBookingAsync(Guid eventId);

        /// <summary>
        /// Получает информацию о бронировании по его уникальному идентификатору.
        /// </summary>
        /// <param name="bookingId">Уникальный идентификатор (GUID) бронирования, которое необходимо получить.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает объект <see cref="Booking"/>,
        /// если бронирование найдено; в противном случае возвращает <c>null</c>.
        /// </returns>
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);

        /// <summary>
        /// Обновляет данные обработанного бронирования по его идентификатору.
        /// </summary>
        /// <param name="bookingId">Уникальный идентификатор (GUID) бронирования, которое необходимо обновить.</param>
        /// <param name="booking">Объект <see cref="BookingProcessedDto"/> с обновлёнными данными бронирования.</param>
        /// <returns>
        /// Асинхронная задача (<see cref="Task{T}"/>), которая возвращает обновлённый объект <see cref="Booking"/>,
        /// если бронирование найдено и успешно обновлено; в противном случае возвращает <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Метод предназначен для обновления бронирований после их обработки системой.
        /// </remarks>
        Task<bool> UpdateProcessedBookingByIdAsync(Guid bookingId, BookingProcessedDto booking);

        /// <summary>
        /// Возвращает коллекцию всех бронирований со статусом «ожидает обработки» (Pending).
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
        /// Возвращает коллекцию всех бронирований со статусом «ожидает обработки» (Pending) (асинхронно).
        /// </summary>
        /// <returns>
        /// Коллекция объектов <see cref="Booking"/>, у которых свойство <see cref="Booking.Status"/>
        /// имеет значение <see cref="BookingStatus.Pending"/>.
        /// </returns>
        /// <remarks>
        /// Метод позволяет получить список всех незавершённых бронирований, которые ещё не были обработаны системой.
        /// </remarks>
        Task<IEnumerable<Booking>> GetPendingAsync();
    }

}
