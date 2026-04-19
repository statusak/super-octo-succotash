using System.ComponentModel.DataAnnotations;

namespace CSCourse.Models
{
    /// <summary>
    /// DTO (Data Transfer Object) для передачи параметров фильтрации мероприятий от клиента.
    /// Используется в запросах API для указания критериев отбора событий.
    /// </summary>
    public class FilterEventDto : IValidatableObject
    {
        /// <summary>
        /// Частичное или полное название мероприятия для поиска (необязательный параметр).
        /// </summary>
        /// <remarks>
        /// Поиск выполняется по частичному совпадению (подстроке) без учёта регистра.
        /// Если параметр не указан или равен null, фильтрация по названию не применяется.
        /// Пример: при значении "конференция" будут найдены мероприятия с названиями
        /// "IT‑конференция 2024", "Весенняя конференция" и т. д.
        /// </remarks>
        public string? Title { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия — нижняя граница диапазона для фильтрации.
        /// </summary>
        /// <remarks>
        /// Будут выбраны мероприятия, у которых startAt >= StartAt.
        /// Если параметр не указан, ограничение по начальной дате не применяется.
        /// </remarks>
        public DateTime? StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия — верхняя граница диапазона для фильтрации.
        /// </summary>
        /// <remarks>
        /// Будут выбраны мероприятия, у которых endAt &lt;= EndAt.
        /// Должна быть позже, чем StartAt (проверяется валидатором).
        /// Если параметр не указан, ограничение по конечной дате не применяется.
        /// </remarks>
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// Выполняет дополнительную валидацию правил для мероприятия.
        /// https://stackoverflow.com/a/38894695:
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndAt < StartAt)
            {
                yield return new ValidationResult("EndAt must be later than StartAt.");
            }
        }

    }
}
