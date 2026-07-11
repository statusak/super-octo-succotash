using CSCourse.Application.Interfaces;
using CSCourse.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CSCourse.Application;

public static class ApplicationCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
