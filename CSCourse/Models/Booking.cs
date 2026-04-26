namespace CSCourse.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected
    }
    public class Booking
    {
        public required Guid Id { get; set; }
        public required Guid EventId { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime? ProcessedAt { get; set; }
    }
}
