namespace CSCourse.Models
{
    /// <summary>
    /// DTO (Data Transfer Object) для передачи данных о бронировании.
    /// Содержит основную информацию: идентификатор, связь с мероприятием, статус, временные метки.
    /// </summary>
    public class BookingResponseDto
    {
        /// <summary>
        /// Уникальный идентификатор бронирования.
        /// </summary>
        /// <remarks>
        /// Генерируется при создании бронирования. Используется для идентификации и поиска записи.
        /// </remarks>
        public required Guid Id { get; set; }

        /// <summary>
        /// Идентификатор мероприятия, к которому относится бронирование.
        /// </summary>
        /// <remarks>
        /// Связывает бронирование с конкретным мероприятием в системе.
        /// </remarks>
        public required Guid EventId { get; set; }

        /// <summary>
        /// Текущий статус бронирования.
        /// </summary>
        /// <remarks>
        /// Определяется значением из перечисления <see cref="BookingStatus"/>.
        /// Может меняться в процессе обработки бронирования.
        /// </remarks>
        public required BookingStatus Status { get; set; }

        /// <summary>
        /// Дата и время создания бронирования.
        /// </summary>
        /// <remarks>
        /// Фиксируется в момент создания записи о бронировании.
        /// Не изменяется в течение жизненного цикла бронирования.
        /// </remarks>
        public required DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата и время обработки бронирования.
        /// </summary>
        /// <remarks>
        /// Устанавливается после завершения обработки бронирования (подтверждения или отклонения).
        /// Имеет значение <c>null</c>, если бронирование ещё не обработано (статус <c>Pending</c>).
        /// </remarks>
        public DateTime? ProcessedAt { get; set; }
    }
}