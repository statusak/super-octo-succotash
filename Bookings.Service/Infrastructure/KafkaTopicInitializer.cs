using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CSCourse.Contracts.Kafka;

namespace Bookings.Service.Infrastructure;

/// <summary>
/// Утилита для инициализации топиков Kafka при старте сервиса бронирований.
/// Гарантирует наличие всех требуемых топиков (booking.created, booking.response и т. д.)
/// с корректными настройками партиций и фактора репликации.
/// </summary>
public static class KafkaTopicInitializer
{   
    /// <summary>
    /// Асинхронно создаёт или проверяет существование всех топиков, объявленных в <see cref="KafkaTopics.All"/>.
    /// Если топик уже существует — ошибка игнорируется; другие ошибки пробрасываются дальше.
    /// </summary>
    /// <param name="bootstrapServers">Список серверов Kafka в формате host:port (например, localhost:9092).</param>
    /// <returns>Задача, завершающаяся после попытки создания/проверки топиков.</returns>
    public static async Task EnsureTopicsAsync(string bootstrapServers)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new AdminClientBuilder(config).Build();

        var topics = KafkaTopics.All
            .Select(name => new TopicSpecification
            {
                Name = name,
                NumPartitions = KafkaTopics.DefaultPartitions,
                ReplicationFactor = KafkaTopics.DefaultReplicationFactor
            })
            .ToArray();

        try
        {
            await adminClient.CreateTopicsAsync(topics, new CreateTopicsOptions
            {
                ValidateOnly = false,
                OperationTimeout = TimeSpan.FromSeconds(60)
            });
            Console.WriteLine("[KAFKA] Topics created/verified successfully.");
        }
        catch (CreateTopicsException e)
        {
            if (!e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                throw;
            
            Console.WriteLine("[KAFKA] Topics already exist.");
        }
    }
}
