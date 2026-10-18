using EventDesk.Application.Abstractions.Persistence;
using EventDesk.Application.Reminders;
using EventDesk.Infrastructure.Persistence;
using EventDesk.Infrastructure.Persistence.Repositories;
using EventDesk.Infrastructure.Notifications;
using EventDesk.Ipc.Abstractions;
using EventDesk.Ipc.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventDeskInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        services.AddDbContext<EventDeskDbContext>(options =>
        {
            Microsoft.Data.Sqlite.SqliteConnectionStringBuilder connectionString = new()
            {
                DataSource = databasePath,
                ForeignKeys = true
            };
            options.UseSqlite(connectionString.ToString());
        });
        services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IReminderStore, ReminderStore>();
        services.AddSingleton<IDesktopReminderClient, NamedPipeDesktopReminderClient>();
        services.AddSingleton<IReminderDispatcher, ReminderDispatcher>();
        return services;
    }
}
