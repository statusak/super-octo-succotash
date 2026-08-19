namespace CSCourse.Contracts.Kafka;

/// <summary>
/// Событие об отмене бронирования.
/// Публикуется через Kafka, когда бронирование успешно отменено.
/// Другой сервис (или фоновый воркер) реагирует на это событие и освобождает места.
/// </summary>
public record BookingCancellation
{
    /// <summary>
    /// Уникальный идентификатор бронирования, которое было отменено.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Идентификатор мероприятия, к которому относилось бронирование.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Причина отмены бронирования.
    /// Позволяет принимающей стороне понять контекст и применить нужную логику (например, вернуть деньги, не возвращать, занести в отчёт).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Дата и время отмены бронирования.
    /// Фиксируется в момент публикации события.
    /// </summary>
    public DateTime CancelledAt { get; init; } = DateTime.UtcNow;
}