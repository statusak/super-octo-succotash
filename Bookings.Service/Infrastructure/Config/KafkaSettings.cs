namespace Bookings.Service.Infrastructure.Config;

/// <summary>
/// Конфигурация клиента Kafka для сервиса бронирований.
/// </summary>
public class KafkaSettings
{
    /// <summary>
    /// Список серверов Kafka в формате host:port (например, localhost:9092).
    /// Используется для подключения продюсера/консьюмера к кластеру Kafka.
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;
}
