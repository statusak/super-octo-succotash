namespace CSCourse.Contracts.Kafka;

/// <summary>
/// Статус ответа на бронирование от сервиса событий.
/// Отражает результат попытки зарезервировать места для конкретного события.
/// </summary>
public enum BookingResponseStatus
{
    /// <summary>
    /// Бронирование подтверждено: места успешно зарезервированы.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Не удалось забронировать: например, мест нет, событие отменено или прошло.
    /// Это бизнес-ошибка, а не сбой системы.
    /// </summary>
    Rejected,

    /// <summary>
    /// Ошибка обработки запроса: сервис событий недоступен, таймаут, внутренняя ошибка.
    /// Требует повторной попытки или эскалации.
    /// </summary>
    Error
}

/// <summary>
/// Ответ сервиса событий на запрос бронирования.
/// Содержит результат попытки зарезервировать места и дополнительные детали.
/// </summary>
public record BookingResponse
{
    /// <summary>
    /// Уникальный идентификатор бронирования (CorrelationId).
    /// Позволяет Booking.Service найти свою запись и обновить статус.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Уникальный идентификатор мероприятия.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Дата и время обработки запроса сервисом событий.
    /// Фиксируется в Event.Service в момент принятия решения (подтверждение/отказ/ошибка).
    /// </summary>
    public required DateTime ProcessedAt { get; init; }

    /// <summary>
    /// Статус результата бронирования.
    /// Confirmed — места зарезервированы.
    /// Rejected — бизнес-правило не выполнено (нет мест, событие закрыто и т.п.).
    /// Error — техническая ошибка обработки.
    /// </summary>
    public required BookingResponseStatus Status { get; init; }

    /// <summary>
    /// Дополнительное сообщение, поясняющее результат.
    /// Для Status = Confirmed может быть пустым.
    /// Для Status = Rejected содержит причину отказа (например, "NoSeats", "EventCancelled").
    /// Для Status = Error содержит описание технической ошибки.
    /// </summary>
    public string? Message { get; set; }
}