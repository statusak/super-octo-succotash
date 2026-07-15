using CSCourse.Infrastructure.DataAccess;
using CSCourse.Infrastructure.Repositories;
using CSCourse.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CSCourse.Application.Services;

namespace CSCourse.Infrastructure;

public static class InfrastructureCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
            {
                o.EnableRetryOnFailure();
            }));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddHostedService<BookingBackgroundService>();


        return services;
    }
}
