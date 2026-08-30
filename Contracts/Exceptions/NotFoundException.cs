namespace CSCourse.Contracts.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда запрашиваемый ресурс не найден.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> без сообщения.
    /// </summary>
    public NotFoundException()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> с указанным сообщением.
    /// </summary>
    /// <param name="path">Путь или идентификатор ресурса, который не был найден.</param>
    public NotFoundException(string path)
        : base(path)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="path">Путь или идентификатор ресурса, который не был найден.</param>
    /// <param name="inner">Внутреннее исключение, которое стало причиной текущего исключения.</param>
    public NotFoundException(string path, Exception inner)
        : base(path, inner)
    {
    }
}
