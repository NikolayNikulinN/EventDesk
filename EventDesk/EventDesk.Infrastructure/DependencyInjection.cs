using EventDesk.Application.Abstractions.Persistence;
using EventDesk.Infrastructure.Persistence;
using EventDesk.Infrastructure.Persistence.Repositories;
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
        return services;
    }
}
