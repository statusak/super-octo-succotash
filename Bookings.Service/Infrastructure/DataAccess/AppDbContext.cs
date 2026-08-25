using Bookings.Service.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Service.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных для сервиса бронирований (Bookings Service).
/// Управляет подключением к БД, отслеживанием сущностей и применением конфигурации моделей.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="AppDbContext"/> с указанными параметрами контекста.
    /// </summary>
    /// <param name="options">Параметры конфигурации DbContext (например, строка подключения и настройки провайдера).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Набор сущностей <see cref="Booking"/> для выполнения операций CRUD через Entity Framework Core.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Настраивает модель данных EF Core: применяет конфигурации из сборки, включая Fluent API.
    /// Используется для определения схем таблиц, ключей, индексов и конвертаций (например, timestamp with time zone).
    /// </summary>
    /// <param name="modelBuilder">Построитель модели, используемый для настройки схемы базы данных.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
