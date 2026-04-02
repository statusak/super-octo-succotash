namespace CSCourse
{
    public class Event
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        private string Description { get; set; }
        public required DateTime StartAt { get; set; }
        public required DateTime EndAt { get; set; }

        public Event(int id, string title, string description, DateTime startAt, DateTime endAt)
        {
            Id = id;
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
        }
        public Event(int id, string title, DateTime startAt, DateTime endAt)
        {
            Id = id;
            Title = title;
            Description = "";
            StartAt = startAt;
            EndAt = endAt;
        }
    }
}
