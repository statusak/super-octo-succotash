namespace CSCourse.Models
{
    public class BookingProcessedDto
    {
        public required BookingStatus Status { get; set; }
        public required DateTime ProcessedAt { get; set; }
    }
}
