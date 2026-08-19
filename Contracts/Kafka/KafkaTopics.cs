namespace CSCourse.Contracts.Kafka;

public static class KafkaTopics
{
    public const string BookingCreated = "booking.created";
    public const string BookingResponse = "booking.response";
    public const string BookingCancellation = "booking.cancellation";

    public const int DefaultPartitions = 3;
    public const short DefaultReplicationFactor = 1;


    public static readonly IReadOnlyList<string> All = new[]
    {
        BookingCreated,
        BookingResponse,
        BookingCancellation
    }.AsReadOnly();
}