namespace CSCourse.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда пытаются забронировать уже прошедшее событие.
/// </summary>
public class BookingForPastEventException : Exception
{
    public BookingForPastEventException(string message) : base(message) { }
}
