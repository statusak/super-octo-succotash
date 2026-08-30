using Bookings.Service.Infrastructure.DataAccess;
using Bookings.Service.Infrastructure.Repositories;
using Bookings.Service.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Design;
using Bookings.Service.Infrastructure.Services;
using Bookings.Service.Infrastructure.Config;

namespace Bookings.Service.Infrastructure;

/// <summary>
/// Методы расширения для регистрации инфраструктурных зависимостей сервиса бронирований.
/// </summary>
public static class InfrastructureCollectionExtensions
{
    /// <summary>
    /// Регистрирует в DI-контейнере контекст базы данных, репозитории,
    /// Kafka-издателя и фоновый сервис бронирований.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    /// <param name="connectionString">Строка подключения к PostgreSQL.</param>
    /// <param name="bootstrapServers">Адреса серверов Kafka в формате host:port.</param>
    /// <returns>Обновлённая коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString, 
        string bootstrapServers)
    {
        // Из-за настройки o.EnableRetryOnFailure() вылетает ошибка,
        // потому что NpgsqlRetryingExecutionStrategy (автоматически включается
        // при использовании UseNpgsql с retry-политикой) не умеет работать
        // с транзакциями, которые начали вручную через BeginTransactionAsync.
        // EF Core требует, чтобы вся транзакция выполнялась внутри стратегии
        // повторных попыток — иначе при transient-ошибке (например, таймаут
        // соединения) повтор не сработает корректно.
        //
        // services.AddDbContext<AppDbContext>(options =>
        //     options.UseNpgsql(connectionString, o =>
        //     {
        //         o.EnableRetryOnFailure();
        //     }));

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<KafkaSettings>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IBookingKafkaPublisher, BookingKafkaPublisher>();
        
        services.AddHostedService<BookingBackgroundService>();
        
        return services;
    }
}

/// <summary>
/// Фабрика для создания <see cref="AppDbContext"/> во время разработки (миграции, scaffolding).
/// Используется инструментами EF Core, когда приложение не запущено.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Создаёт экземпляр <see cref="AppDbContext"/> с параметрами из переменной окружения
    /// или строкой подключения по умолчанию.
    /// </summary>
    /// <param name="args">Аргументы командной строки (не используются).</param>
    /// <returns>Настроенный экземпляр <see cref="AppDbContext"/>.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=cscourse_dev;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure();
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
