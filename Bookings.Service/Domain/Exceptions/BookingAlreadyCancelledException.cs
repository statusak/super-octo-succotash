namespace Bookings.Service.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда пытаются выполнить операцию над бронированием, которое уже отменено.
/// </summary>
public class BookingAlreadyCancelledException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingAlreadyCancelledException"/> без сообщения.
    /// </summary>
    public BookingAlreadyCancelledException() { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingAlreadyCancelledException"/> с указанным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение, описывающее причину исключения.</param>
    public BookingAlreadyCancelledException(string message) : base(message) { }
}
