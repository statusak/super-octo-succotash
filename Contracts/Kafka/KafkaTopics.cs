namespace CSCourse.Contracts.Kafka;

/// <summary>
/// Коллекция констант для работы с топиками Kafka в проекте CSCourse.
/// Содержит имена топиков, настройки по умолчанию и список всех топиков.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Топик для событий о создании бронирования.
    /// </summary>
    public const string BookingCreated = "booking.created";

    /// <summary>
    /// Топик для ответов на запросы бронирования.
    /// </summary>
    public const string BookingResponse = "booking.response";

    /// <summary>
    /// Топик для событий об отмене бронирования.
    /// </summary>
    public const string BookingCancellation = "booking.cancellation";

    /// <summary>
    /// Количество партиций по умолчанию для топиков Kafka.
    /// </summary>
    public const int DefaultPartitions = 3;

    /// <summary>
    /// Фактор репликации по умолчанию для топиков Kafka.
    /// </summary>
    public const short DefaultReplicationFactor = 1;

    /// <summary>
    /// Список всех топиков Kafka, используемых в проекте.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        BookingCreated,
        BookingResponse,
        BookingCancellation
    }.AsReadOnly();
}
