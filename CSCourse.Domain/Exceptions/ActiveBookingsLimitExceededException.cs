namespace CSCourse.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда у пользователя превышен лимит активных броней.
/// </summary>
public class ActiveBookingsLimitExceededException : Exception
{
    public ActiveBookingsLimitExceededException(string message) : base(message) { }
}