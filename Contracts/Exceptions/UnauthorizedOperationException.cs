namespace CSCourse.Contracts.Exceptions;

/// <summary>
/// Выбрасывается, когда у текущего пользователя нет прав на выполнение операции.
/// </summary>
public class UnauthorizedOperationException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UnauthorizedOperationException"/> с указанным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение, описывающее причину отсутствия прав.</param>
    public UnauthorizedOperationException(string message) : base(message) { }
}
