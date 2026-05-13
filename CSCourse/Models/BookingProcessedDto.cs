namespace CSCourse.Models
{
    /// <summary>
    /// DTO (Data Transfer Object) для передачи данных об обработанном бронировании.
    /// Используется при обновлении статуса бронирования после его обработки системой.
    /// </summary>
    public class BookingProcessedDto
    {
        /// <summary>
        /// Новый статус бронирования после обработки.
        /// </summary>
        /// <remarks>
        /// Определяется значением из перечисления <see cref="BookingStatus"/>.
        /// Возможные значения:
        /// <list type="bullet">
        ///   <item><description><c>Confirmed</c> — бронирование успешно подтверждено.</description></item>
        ///   <item><description><c>Rejected</c> — бронирование отклонено по какой‑либо причине.</description></item>
        /// </list>
        /// Поле обязательно для заполнения.
        /// </remarks>
        public required BookingStatus Status { get; set; }

        /// <summary>
        /// Дата и время завершения обработки бронирования.
        /// </summary>
        /// <remarks>
        /// Фиксирует момент, когда система завершила обработку бронирования
        /// и установила его финальный статус (<c>Confirmed</c> или <c>Rejected</c>).
        /// Должна соответствовать текущей дате и времени на момент обновления.
        /// Поле обязательно для заполнения.
        /// </remarks>
        public required DateTime ProcessedAt { get; set; }
    }
}