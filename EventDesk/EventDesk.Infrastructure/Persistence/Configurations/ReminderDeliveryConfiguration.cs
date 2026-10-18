using EventDesk.Constants;
using EventDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventDesk.Infrastructure.Persistence.Configurations;

public sealed class ReminderDeliveryConfiguration : IEntityTypeConfiguration<ReminderDeliveryRecord>
{
    public void Configure(EntityTypeBuilder<ReminderDeliveryRecord> builder)
    {
        builder.ToTable(DatabaseConstants.ReminderDeliveriesTable);
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventTitle).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Channel).HasConversion<int>();
        builder.Property(item => item.Status).HasConversion<int>();
        builder.Property(item => item.DueAtUtc).HasConversion(item => item.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.Property(item => item.NextAttemptAtUtc).HasConversion(item => item.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.Property(item => item.LeaseUntilUtc).HasConversion(
            item => item.HasValue ? item.Value.UtcTicks : (long?)null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);
        builder.Property(item => item.DeliveredAtUtc).HasConversion(
            item => item.HasValue ? item.Value.UtcTicks : (long?)null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);
        builder.Property(item => item.LastErrorCode).HasMaxLength(100);
        builder.HasIndex(item => new { item.EventId, item.Channel, item.DueAtUtc }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.NextAttemptAtUtc });
        builder.HasMany<ReminderAttemptRecord>().WithOne().HasForeignKey(item => item.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
