using Events.Service.Application.Interfaces;
using Events.Service.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Service.Application;

public static class ApplicationCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
