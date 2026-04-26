using System.ComponentModel.DataAnnotations;

namespace CSCourse.Models
{
    /// <summary>
    /// Класс Dto, содержащий информацию о мероприятии.
    /// </summary>
    public class EventDto : IValidatableObject
    {
        /// <summary>
        /// Название мероприятия.
        /// </summary>
        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }

        /// <summary>
        /// Описание мероприятия (может быть пустым).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия.
        /// </summary>
        [Required(ErrorMessage = "StartAt is required.")]
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия.
        /// </summary>
        [Required(ErrorMessage = "EndAt is required.")]
        public required DateTime EndAt { get; set; }

        /// <summary>
        /// Выполняет дополнительную валидацию правил для мероприятия.
        /// https://stackoverflow.com/a/38894695:
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndAt <= StartAt)
            {
                yield return new ValidationResult("EndAt must be later than StartAt.");
            }
        }
    }
}
