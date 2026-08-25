using Bookings.Service.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCourse.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности <see cref="Booking"/> для Entity Framework Core.
/// Определяет маппинг на таблицу "bookings", типы колонок, ограничения и конвертации.
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Настраивает схему таблицы и свойства сущности <see cref="Booking"/>.
    /// Устанавливает первичный ключ, типы колонок (включая timestamp with time zone),
    /// обязательные поля и конвертацию статуса в строку.
    /// </summary>
    /// <param name="builder">Построитель типа сущности для настройки модели EF Core.</param>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        // https://stackoverflow.com/q/47013752
        // ID задаётся вручную (не генерируется БД), т.к. мы используем Guid.NewGuid() в репозитории
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.HasKey(b => b.Id);

        builder.Property(b => b.EventId).IsRequired();
        builder.Property(b => b.UserId).IsRequired();

        // Статус хранится в БД как строка, конвертация выполняется автоматически
        builder.Property(b => b.Status).IsRequired().HasConversion<string>();

        // PostgreSQL: timestamp with time zone — важно для корректной работы с часовыми поясами
        builder.Property(b => b.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(b => b.ProcessedAt).HasColumnType("timestamp with time zone");
    }
}
