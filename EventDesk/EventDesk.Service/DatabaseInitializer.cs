using EventDesk.Constants;
using EventDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventDesk.Service;

internal static class DatabaseInitializer
{
    public static string GetDatabasePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        DatabaseConstants.ApplicationDataDirectoryName,
        DatabaseConstants.DataDirectoryName,
        DatabaseConstants.DatabaseFileName);

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        EventDeskDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventDeskDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using System.Data.Common.DbCommand command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = DatabaseConstants.SetWalJournalModeSql;
            object? mode = await command.ExecuteScalarAsync(cancellationToken);
            if (!string.Equals(mode?.ToString(), DatabaseConstants.WalJournalMode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(DatabaseConstants.WalModeNotEnabled);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
