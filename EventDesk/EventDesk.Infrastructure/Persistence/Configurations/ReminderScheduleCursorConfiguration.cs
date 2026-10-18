using EventDesk.Constants;
using EventDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventDesk.Infrastructure.Persistence.Configurations;

public sealed class ReminderScheduleCursorConfiguration : IEntityTypeConfiguration<ReminderScheduleCursor>
{
    public void Configure(EntityTypeBuilder<ReminderScheduleCursor> builder)
    {
        builder.ToTable(DatabaseConstants.ReminderScheduleCursorTable);
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ThroughUtc).HasConversion(item => item.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
    }
}
