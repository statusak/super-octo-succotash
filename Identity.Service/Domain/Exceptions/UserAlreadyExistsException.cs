namespace Identity.Service.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда пользователь уже существует в системе.
/// Обычно используется при попытке регистрации или создания учётной записи с уже занятым идентификатором/логином.
/// </summary>
public class UserAlreadyExistsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UserAlreadyExistsException"/> без сообщения.
    /// </summary>
    public UserAlreadyExistsException()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UserAlreadyExistsException"/> с указанным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение, описывающее причину исключения.</param>
    public UserAlreadyExistsException(string message) : base(message)
    {
    }
}
