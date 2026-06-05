namespace CSCourse.Models
{
    /// <summary>
    /// Модель бронирования. Представляет данные о бронировании мероприятия.
    /// Содержит основную информацию: идентификатор, связь с мероприятием, статус, временные метки.
    /// Используется как DTO для создания в БД
    /// </summary>
    public class BookingRepositoryCreateDto
    {
        /// <summary>
        /// Идентификатор мероприятия, к которому относится бронирование.
        /// </summary>
        /// <remarks>
        /// Связывает бронирование с конкретным мероприятием в системе.
        /// </remarks>
        /// 
        // TODO: Во Fluent API Указать, что это ForeignKey для EventID
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
