namespace CSCourse.Application.Models
{
    /// <summary>
    /// Класс, содержащий информацию о мероприятии.
    /// </summary>
    public class EventInfoDto
    {
        /// <summary>
        /// Уникальный идентификатор мероприятия.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Название мероприятия.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Описание мероприятия (может быть пустым).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Общее количество мест на мероприятии.
        /// Определяет максимальную вместимость события (например, вместимость аудитории или лимит регистраций для онлайн-формата).
        /// Должно быть положительным числом (минимум 1).
        /// </summary>
        public required int TotalSeats { get; set; }

        /// <summary>
        /// Количество свободных мест на мероприятии.
        /// Динамическое значение, которое уменьшается при регистрации участников и увеличивается при отмене регистрации.
        /// </summary>
        public required int AvailableSeats { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия.
        /// </summary>
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия.
        /// </summary>
        public required DateTime EndAt { get; set; }
    }
}
