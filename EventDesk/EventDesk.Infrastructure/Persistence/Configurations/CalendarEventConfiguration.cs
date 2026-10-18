using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EventDesk.Infrastructure.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        ValueConverter<DateTimeOffset, long> utcTicksConverter = new(
            value => value.ToUniversalTime().Ticks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        builder.ToTable("CalendarEvents");
        builder.HasKey(calendarEvent => calendarEvent.Id);
        builder.Property(calendarEvent => calendarEvent.Id).ValueGeneratedNever();
        builder.Property(calendarEvent => calendarEvent.Title).IsRequired().HasMaxLength(500);
        builder.Property(calendarEvent => calendarEvent.Description).HasMaxLength(2000);
        builder.Property(calendarEvent => calendarEvent.ColorHex).HasMaxLength(7);
        builder.Property(calendarEvent => calendarEvent.RecurrenceKind).HasConversion<int>().IsRequired();
        builder.Property(calendarEvent => calendarEvent.RecurrenceMonth);
        builder.Property(calendarEvent => calendarEvent.RecurrenceDay);
        builder.Property(calendarEvent => calendarEvent.NotificationIntervalMinutes);
        builder.Property(calendarEvent => calendarEvent.StartsAtUtc)
            .HasColumnName("StartsAtUtcTicks").HasConversion(utcTicksConverter).IsRequired();
        builder.Property(calendarEvent => calendarEvent.EndsAtUtc)
            .HasColumnName("EndsAtUtcTicks").HasConversion(utcTicksConverter).IsRequired();
        builder.Property(calendarEvent => calendarEvent.TimeZoneId).IsRequired().HasMaxLength(200);
        builder.Property(calendarEvent => calendarEvent.IsEnabled).IsRequired();
        builder.Property(calendarEvent => calendarEvent.IsDeleted).IsRequired();
        builder.HasIndex(calendarEvent => new { calendarEvent.IsDeleted, calendarEvent.StartsAtUtc });
        builder.HasIndex(calendarEvent => new { calendarEvent.IsDeleted, calendarEvent.EndsAtUtc });
        builder.HasIndex(calendarEvent => new { calendarEvent.IsDeleted, calendarEvent.RecurrenceKind });

        builder.OwnsMany(calendarEvent => calendarEvent.Reminders, reminder =>
        {
            reminder.ToTable("ReminderRules");
            reminder.WithOwner().HasForeignKey("EventId");
            reminder.Property<Guid>("Id").ValueGeneratedOnAdd();
            reminder.HasKey("Id");
            reminder.HasIndex("EventId");
            reminder.Property(rule => rule.LeadTime)
                .HasColumnName("LeadTimeTicks")
                .HasConversion(value => value.Ticks, value => TimeSpan.FromTicks(value))
                .IsRequired();
            reminder.Property(rule => rule.Channel).HasConversion<int>().IsRequired();
            reminder.Property(rule => rule.OccurrenceCount).IsRequired();
            reminder.Property(rule => rule.RepeatInterval)
                .HasColumnName("RepeatIntervalTicks")
                .HasConversion(value => value!.Value.Ticks, value => TimeSpan.FromTicks(value));
        });

        builder.Navigation(calendarEvent => calendarEvent.Reminders)
            .HasField("reminders")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
