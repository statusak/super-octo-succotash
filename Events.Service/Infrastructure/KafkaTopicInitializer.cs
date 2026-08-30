using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CSCourse.Contracts.Kafka;

namespace Events.Service.Infrastructure;

public static class KafkaTopicInitializer
{   
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
