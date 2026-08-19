using Bookings.Service.Domain.Models;
using CSCourse.Contracts.Models;

namespace Bookings.Service.Application.Interfaces
{
    /// <summary>
    /// Сервис для управления бронированиями в событийно‑ориентированной архитектуре.
    /// Отвечает за инициацию бронирований, отмену, чтение с проверкой прав и получение списков для фоновой обработки.
    /// </summary>
    public interface IBookingService
    {
        /// <summary>
        /// Инициирует бронирование: создаёт запись в БД со статусом Pending и отправляет событие BookingCreated в Kafka.
        /// Не проверяет существование мероприятия заранее — это обрабатывается асинхронно Event.Service.
        /// </summary>
        /// <param name="eventId">Идентификатор мероприятия.</param>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Созданный объект бронирования.</returns>
        /// <exception cref="ConflictException">Если достигнут лимит бронирований пользователя или иные бизнес‑конфликты.</exception>
        Task<Booking> InitiateBookingAsync(Guid eventId, Guid userId);

        /// <summary>
        /// Получает бронирование по ID с проверкой прав: владелец или администратор может получить данные.
        /// </summary>
        /// <param name="bookingId">Идентификатор бронирования.</param>
        /// <param name="userId">Идентификатор текущего пользователя (для проверки прав).</param>
        /// <param name="role">Роль текущего пользователя (Owner/Admin).</param>
        /// <returns>Объект бронирования или null, если нет доступа.</returns>
        /// <exception cref="UnauthorizedOperationException">Если пользователь пытается получить чужую бронь без прав администратора.</exception>
        /// <exception cref="NotFoundException">Если бронирование не найдено.</exception>
        Task<Booking?> GetBookingByIdAsync(Guid bookingId, Guid userId, AccountRole role);

        /// <summary>
        /// Инициирует отмену бронирования: ставит статус Cancelling и отправляет событие BookingCancellation в Kafka.
        /// Проверка прав и валидность состояния (нельзя отменить подтверждённую бронь) выполняется внутри метода.
        /// </summary>
        /// <param name="bookingId">Идентификатор бронирования.</param>
        /// <param name="userId">Идентификатор пользователя, инициирующего отмену.</param>
        /// <param name="role">Роль пользователя (Owner/Admin).</param>
        /// <exception cref="NotFoundException">Если бронирование не найдено.</exception>
        /// <exception cref="UnauthorizedOperationException">Если пользователь не имеет прав на отмену.</exception>
        /// <exception cref="InvalidOperationException">Если отмена невозможна из‑за текущего статуса бронирования.</exception>
        Task CancelBookingAsync(Guid bookingId, Guid userId, AccountRole role);

        /// <summary>
        /// Обновляет статус бронирования на основе входящего события (например, от Event.Service).
        /// Вызывается из фонового сервиса (BookingBackgroundService) при обработке сообщений из Kafka.
        /// </summary>
        /// <param name="bookingId">Идентификатор бронирования.</param>
        /// <param name="status">Новый статус бронирования.</param>
        /// <param name="message">Опциональное сообщение (причина отклонения, комментарий и т. п.).</param>
        /// <exception cref="NotFoundException">Если бронирование не найдено.</exception>
        Task UpdateBookingStatusAsync(Guid bookingId, BookingStatus status, string? message = null);

        /// <summary>
        /// Возвращает список бронирований со статусом Pending для обработки фоновым воркером.
        /// </summary>
        /// <returns>Коллекция бронирований в статусе Pending.</returns>
        Task<IEnumerable<Booking>> GetPendingAsync();

        /// <summary>
        /// Возвращает список бронирований со статусом Cancelling для обработки фоновым воркером.
        /// </summary>
        /// <returns>Коллекция бронирований в статусе Cancelling.</returns>
        Task<IEnumerable<Booking>> GetCancellingAsync();
    }
}
