namespace CSCourse.Models
{
    public class FilterEvent
    {
        public required string Title { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
