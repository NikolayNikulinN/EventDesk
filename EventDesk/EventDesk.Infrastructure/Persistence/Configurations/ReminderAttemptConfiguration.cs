using EventDesk.Constants;
using EventDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventDesk.Infrastructure.Persistence.Configurations;

public sealed class ReminderAttemptConfiguration : IEntityTypeConfiguration<ReminderAttemptRecord>
{
    public void Configure(EntityTypeBuilder<ReminderAttemptRecord> builder)
    {
        builder.ToTable(DatabaseConstants.ReminderAttemptsTable);
        builder.HasKey(item => item.Id);
        builder.Property(item => item.StartedAtUtc).HasConversion(item => item.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.Property(item => item.FinishedAtUtc).HasConversion(
            item => item.HasValue ? item.Value.UtcTicks : (long?)null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);
        builder.Property(item => item.Status).HasConversion<int>();
        builder.Property(item => item.ErrorCode).HasMaxLength(100);
        builder.HasIndex(item => new { item.DeliveryId, item.Number }).IsUnique();
    }
}
