using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;
using Xunit;

namespace EventDesk.Domain.Tests.Events;

public sealed class TimeZoneTests
{
    [Fact]
    public void Create_SpringForwardInterval_UsesElapsedUtcDuration()
    {
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)),
            "Europe/Prague");

        Assert.Equal(TimeSpan.FromHours(1), calendarEvent.EndsAtUtc - calendarEvent.StartsAtUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Create_NonexistentSpringForwardLocalTime_Throws(int offsetHours)
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 3, 29, 2, 30, 0, TimeSpan.FromHours(offsetHours)),
            new DateTimeOffset(2026, 3, 29, 4, 0, 0, TimeSpan.FromHours(2)),
            "Europe/Prague"));
    }

    [Fact]
    public void Create_AutumnRepeatedHour_BothExplicitOffsetsAreValidAndDistinct()
    {
        DateTimeOffset end = new(2026, 10, 25, 3, 30, 0, TimeSpan.FromHours(1));
        CalendarEvent earlier = CalendarEvent.Create(
            Guid.NewGuid(), "First occurrence",
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)), end,
            "Europe/Prague");
        CalendarEvent later = CalendarEvent.Create(
            Guid.NewGuid(), "Second occurrence",
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1)), end,
            "Europe/Prague");

        Assert.Equal(TimeSpan.FromHours(1), later.StartsAtUtc - earlier.StartsAtUtc);
    }

    [Fact]
    public void Create_AutumnWallTimeMovesBackward_ButUtcIntervalIsPositive()
    {
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 10, 25, 2, 45, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 10, 25, 2, 15, 0, TimeSpan.FromHours(1)),
            "Europe/Prague");

        Assert.Equal(TimeSpan.FromMinutes(30), calendarEvent.EndsAtUtc - calendarEvent.StartsAtUtc);
    }

    [Fact]
    public void Create_WallTimeMovesForwardButUtcEndPrecedesStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 10, 25, 2, 45, 0, TimeSpan.FromHours(2)),
            "Europe/Prague"));
    }

    [Fact]
    public void Create_OffsetDoesNotMatchZone_Throws()
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.FromHours(1)),
            "Europe/Prague"));
    }

    [Fact]
    public void Create_UnknownTimeZone_Throws()
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero),
            "Invalid/NotAZone"));
    }

    [Fact]
    public void ReminderSchedule_AcrossSpringForward_UsesElapsedUtcIntervals()
    {
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), "Meeting",
            new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 3, 29, 4, 0, 0, TimeSpan.FromHours(2)),
            "Europe/Prague");
        calendarEvent.AddReminder(ReminderRule.Create(
            TimeSpan.FromHours(2), ReminderChannel.Desktop, occurrenceCount: 3,
            repeatInterval: TimeSpan.FromHours(1)));

        DateTimeOffset[] triggers = calendarEvent.GetReminderSchedule()
            .Select(occurrence => occurrence.TriggerAtUtc).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 3, 28, 23, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero)
            ],
            triggers);
    }
}
