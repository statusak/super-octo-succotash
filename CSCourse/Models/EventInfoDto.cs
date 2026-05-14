namespace CSCourse.Models
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

        public required int TotalSeats { get; set; }
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
