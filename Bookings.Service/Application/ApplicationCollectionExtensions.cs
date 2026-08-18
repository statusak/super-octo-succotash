using Bookings.Service.Application.Interfaces;
using Bookings.Service.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Service.Application;

public static class ApplicationCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
