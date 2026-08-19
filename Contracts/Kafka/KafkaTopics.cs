namespace CSCourse.Contracts.Kafka;

public static class KafkaTopics
{
    public const string BookingCreated = "booking.created";
    public const string BookingResponse = "booking.response";
    public const string BookingCancellation = "booking.cancellation";

    public static readonly IReadOnlyList<string> All = new[]
    {
        BookingCreated,
        BookingResponse,
        BookingCancellation
    }.AsReadOnly();
}