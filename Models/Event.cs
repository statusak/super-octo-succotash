namespace CSCourse.Models
{
    public class Event
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; }
        public required DateTime StartAt { get; set; }
        public required DateTime EndAt { get; set; }

    }
}
