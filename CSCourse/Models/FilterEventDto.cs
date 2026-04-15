using CSCourse.Validators;
using System.ComponentModel.DataAnnotations;

namespace CSCourse.Models
{
    public class FilterEventDto
    {
        public string? Title { get; set; }
        public DateTime? StartAt { get; set; }

        [DateTimeValidator(ErrorMessage = "EndAt must be later than StartAt.")]
        public DateTime? EndAt { get; set; }
    }
}
