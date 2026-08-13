namespace CSCourse.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда у текущего пользователя нет прав на выполнение операции.
/// </summary>
public class UnauthorizedOperationException : Exception
{
    public UnauthorizedOperationException(string message) : base(message) { }
}