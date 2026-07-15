using System.ComponentModel.DataAnnotations;

namespace CSCourse.Application.Models
{
    /// <summary>
    /// Класс Dto, содержащий информацию о мероприятии.
    /// Используется для передачи данных при создании нового события, включает правила валидации.
    /// </summary>
    public class EventCreateDto : IValidatableObject
    {
        /// <summary>
        /// Название мероприятия.
        /// Должно быть кратким и информативным (например, «Лекция по алгоритмам»), обязательно для заполнения.
        /// </summary>
        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }

        /// <summary>
        /// Описание мероприятия (может быть пустым).
        /// Подробная информация о содержании события: темы, формат, особенности. Допустимо отсутствие значения.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Общее количество мест на мероприятии.
        /// Определяет максимальную вместимость события (например, вместимость аудитории или лимит регистраций для онлайн-формата).
        /// Должно быть положительным числом (минимум 1).
        /// </summary>
        [Required(ErrorMessage = "TotalSeats is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "TotalSeats must be at least 1.")]
        public required int TotalSeats { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия.
        /// Указывает момент, когда событие стартует (в формате UTC или локального времени в зависимости от настроек системы).
        /// Обязательно для заполнения.
        /// </summary>
        [Required(ErrorMessage = "StartAt is required.")]
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия.
        /// Фиксирует момент завершения события; должна быть позже StartAt, проверка выполняется в методе Validate.
        /// Обязательно для заполнения.
        /// </summary>
        [Required(ErrorMessage = "EndAt is required.")]
        public required DateTime EndAt { get; set; }

        /// <summary>
        /// Выполняет дополнительную валидацию правил для мероприятия.
        /// Проверяет, что время окончания события (EndAt) наступает позже времени начала (StartAt).
        /// Если условие не выполнено, возвращается ошибка валидации.
        /// https://stackoverflow.com/a/38894695
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
