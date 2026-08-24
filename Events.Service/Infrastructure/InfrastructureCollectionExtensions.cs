using Events.Service.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Design;
using Events.Service.Infrastructure.Services;
using Events.Service.Infrastructure.DataAccess;
using Events.Service.Infrastructure.Config;
using Events.Service.Infrastructure.Repositories;

namespace Events.Service.Infrastructure;

public static class InfrastructureCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString, 
        string bootstrapServers)
    {
        /// Из-за настройки o.EnableRetryOnFailure() вылетает ошибка, 
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
        ///
        /// 
        

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<KafkaSettings>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });


        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventKafkaPublisher, EventKafkaPublisher>();
        
        services.AddHostedService<EventBackgroundService>();
        
        return services;
    }
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
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

