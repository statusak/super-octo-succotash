namespace CSCourse.Models
{
    public class PaginatedResult
    {
        public required List<Event> Events { get; set ;}
        public required int CountEvents { get; set; }
    }
}
