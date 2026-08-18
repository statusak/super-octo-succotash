namespace CSCourse.Contracts.Models;

/// <summary>
/// Событие об отмене бронирования.
/// Публикуется через Kafka, когда бронирование успешно отменено.
/// Другой сервис (или фоновый воркер) реагирует на это событие и освобождает места.
/// </summary>
public record BookingCancelledEvent
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
    /// Идентификатор пользователя, который отменил бронирование.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Дата и время отмены бронирования.
    /// </summary>
    public required DateTime CancelledAt { get; init; }
}
