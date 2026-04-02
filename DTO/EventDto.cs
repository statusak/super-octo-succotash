using System.ComponentModel.DataAnnotations;

namespace CSCourse.Dto
{
    public class EventDto
    {
        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "StartAt is required.")]
        public required DateTime StartAt { get; set; }
        
        [Required(ErrorMessage = "EndAt is required.")]
        public required DateTime EndAt { get; set; }
    }
}
