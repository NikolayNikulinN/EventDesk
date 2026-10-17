using EventDesk.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EventDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddEventDeskApplication(this IServiceCollection services)
    {
        services.AddScoped<CalendarEventService>();
        return services;
    }
}
