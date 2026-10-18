using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;
using Xunit;

namespace EventDesk.Domain.Tests.Events;

public sealed class CalendarEventTests
{
    [Fact]
    public void Create_ValidInput_StoresUtcInstantsAndStartsEnabled()
    {
        DateTimeOffset start = new(2026, 10, 16, 9, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset end = new(2026, 10, 16, 10, 0, 0, TimeSpan.FromHours(2));

        CalendarEvent calendarEvent = CalendarEvent.Create(Guid.NewGuid(), "  Team meeting  ", start, end, "Europe/Prague");

        Assert.Equal("Team meeting", calendarEvent.Title);
        Assert.Equal(start.ToUniversalTime(), calendarEvent.StartsAtUtc);
        Assert.Equal(end.ToUniversalTime(), calendarEvent.EndsAtUtc);
        Assert.Equal(TimeSpan.Zero, calendarEvent.StartsAtUtc.Offset);
        Assert.Equal("Europe/Prague", calendarEvent.TimeZoneId);
        Assert.True(calendarEvent.IsEnabled);
        Assert.False(calendarEvent.IsDeleted);
    }

    [Fact]
    public void Create_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.Empty,
            "Meeting",
            new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero),
            "UTC"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), title,
            new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero),
            "UTC"));
    }

    [Fact]
    public void Create_EndNotAfterStart_Throws()
    {
        DateTimeOffset start = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(Guid.NewGuid(), "Meeting", start, start, "UTC"));
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(Guid.NewGuid(), "Meeting", start, start.AddTicks(-1), "UTC"));
    }

    [Fact]
    public void Reschedule_InvalidInput_PreservesPreviousSchedule()
    {
        CalendarEvent calendarEvent = CreateEvent();
        DateTimeOffset originalStart = calendarEvent.StartsAtUtc;
        DateTimeOffset originalEnd = calendarEvent.EndsAtUtc;

        Assert.Throws<ArgumentException>(() => calendarEvent.Reschedule(
            new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero),
            "UTC"));

        Assert.Equal(originalStart, calendarEvent.StartsAtUtc);
        Assert.Equal(originalEnd, calendarEvent.EndsAtUtc);
        Assert.Equal("UTC", calendarEvent.TimeZoneId);
    }

    [Fact]
    public void UpdateTitle_ValidInput_TrimsTitle()
    {
        CalendarEvent calendarEvent = CreateEvent();

        calendarEvent.UpdateTitle("  Updated meeting  ");

        Assert.Equal("Updated meeting", calendarEvent.Title);
    }

    [Fact]
    public void UpdateDetails_ValidInput_StoresDescriptionAndColor()
    {
        CalendarEvent calendarEvent = CreateEvent();

        calendarEvent.UpdateDetails("  Reminder message  ", "#259b70");

        Assert.Equal("Reminder message", calendarEvent.Description);
        Assert.Equal("#259B70", calendarEvent.ColorHex);
    }

    [Fact]
    public void UpdateDetails_InvalidColor_PreservesPreviousDetails()
    {
        CalendarEvent calendarEvent = CreateEvent();
        calendarEvent.UpdateDetails("Original message", "#425EBA");

        Assert.Throws<ArgumentException>(() => calendarEvent.UpdateDetails("Changed message", "blue"));

        Assert.Equal("Original message", calendarEvent.Description);
        Assert.Equal("#425EBA", calendarEvent.ColorHex);
    }

    [Fact]
    public void AddReminder_DuplicateRule_Throws()
    {
        CalendarEvent calendarEvent = CreateEvent();
        ReminderRule first = ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Desktop);
        ReminderRule equivalent = ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Desktop);

        calendarEvent.AddReminder(first);

        Assert.Throws<InvalidOperationException>(() => calendarEvent.AddReminder(equivalent));
        Assert.Single(calendarEvent.Reminders);
    }

    [Fact]
    public void RemoveReminder_ExistingRule_RemovesIt()
    {
        CalendarEvent calendarEvent = CreateEvent();
        ReminderRule reminder = ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Email);
        calendarEvent.AddReminder(reminder);

        Assert.True(calendarEvent.RemoveReminder(reminder));
        Assert.False(calendarEvent.RemoveReminder(reminder));
        Assert.Empty(calendarEvent.Reminders);
    }

    [Fact]
    public void GetReminderSchedule_MultipleChannels_ReturnsChronologicalUtcOccurrences()
    {
        CalendarEvent calendarEvent = CreateEvent();
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Desktop));
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromMinutes(30), ReminderChannel.Email));

        IReadOnlyList<ReminderOccurrence> schedule = calendarEvent.GetReminderSchedule();

        Assert.Equal(2, schedule.Count);
        Assert.Equal(ReminderChannel.Email, schedule[0].Channel);
        Assert.Equal(calendarEvent.StartsAtUtc.AddMinutes(-30), schedule[0].TriggerAtUtc);
        Assert.Equal(ReminderChannel.Desktop, schedule[1].Channel);
        Assert.Equal(calendarEvent.StartsAtUtc.AddMinutes(-15), schedule[1].TriggerAtUtc);
    }

    [Fact]
    public void DisableAndEnable_ControlReminderSchedule()
    {
        CalendarEvent calendarEvent = CreateEvent();
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Desktop));

        calendarEvent.Disable();
        Assert.False(calendarEvent.IsEnabled);
        Assert.Empty(calendarEvent.GetReminderSchedule());

        calendarEvent.Enable();
        Assert.True(calendarEvent.IsEnabled);
        Assert.Single(calendarEvent.GetReminderSchedule());
    }

    [Fact]
    public void Delete_DisablesEventAndPreventsFurtherChanges()
    {
        CalendarEvent calendarEvent = CreateEvent();
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromMinutes(15), ReminderChannel.Desktop));

        calendarEvent.Delete();

        Assert.True(calendarEvent.IsDeleted);
        Assert.False(calendarEvent.IsEnabled);
        Assert.Empty(calendarEvent.GetReminderSchedule());
        Assert.Single(calendarEvent.Reminders);
        Assert.Throws<InvalidOperationException>(() => calendarEvent.Enable());
        Assert.Throws<InvalidOperationException>(() => calendarEvent.UpdateTitle("New title"));
        Assert.Throws<InvalidOperationException>(() => calendarEvent.AddReminder(
            ReminderRule.Create(TimeSpan.FromMinutes(10), ReminderChannel.Email)));
    }

    [Fact]
    public void Reschedule_BeforeExistingReminderCanFit_ThrowsWithoutMutation()
    {
        CalendarEvent calendarEvent = CreateEvent();
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromDays(2), ReminderChannel.Desktop));
        DateTimeOffset originalStart = calendarEvent.StartsAtUtc;

        Assert.Throws<ArgumentException>(() => calendarEvent.Reschedule(
            new DateTimeOffset(1, 1, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1, 1, 2, 10, 0, 0, TimeSpan.Zero),
            "UTC"));

        Assert.Equal(originalStart, calendarEvent.StartsAtUtc);
    }

    [Fact]
    public void YearlyFebruary29_SkipsNonLeapYears_AndUsesThreeNotificationTimes()
    {
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), "Birthday", new DateTimeOffset(2028, 2, 29, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2028, 2, 29, 17, 0, 0, TimeSpan.Zero), "UTC",
            recurrenceKind: EventRecurrenceKind.Yearly, recurrenceMonth: 2, recurrenceDay: 29,
            notificationIntervalMinutes: 180);

        Assert.Empty(calendarEvent.GetRecurringNotificationTimes(new DateOnly(2027, 2, 28)));
        Assert.False(calendarEvent.OccursOn(new DateOnly(2027, 3, 1)));
        Assert.Equal(new[] { 11, 14, 17 },
            calendarEvent.GetRecurringNotificationTimes(new DateOnly(2032, 2, 29))
                .Select(time => time.Hour));
        calendarEvent.Disable();
        Assert.Empty(calendarEvent.GetRecurringNotificationTimes(new DateOnly(2032, 2, 29)));
    }

    [Fact]
    public void MonthlyThirtyFirst_SkipsShortMonths()
    {
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), "Monthly", new DateTimeOffset(2026, 1, 31, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 17, 0, 0, TimeSpan.Zero), "UTC",
            recurrenceKind: EventRecurrenceKind.Monthly, recurrenceDay: 31,
            notificationIntervalMinutes: 180);

        Assert.False(calendarEvent.OccursOn(new DateOnly(2026, 2, 28)));
        Assert.Empty(calendarEvent.GetRecurringNotificationTimes(new DateOnly(2026, 2, 28)));
        Assert.Equal(3, calendarEvent.GetRecurringNotificationTimes(new DateOnly(2026, 3, 31)).Count);
    }

    [Fact]
    public void RecurringNotificationWindow_MustAlignWithInterval()
    {
        Assert.Throws<ArgumentException>(() => CalendarEvent.Create(
            Guid.NewGuid(), "Birthday", new DateTimeOffset(2028, 2, 29, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2028, 2, 29, 17, 0, 0, TimeSpan.Zero), "UTC",
            recurrenceKind: EventRecurrenceKind.Yearly, recurrenceMonth: 2, recurrenceDay: 29,
            notificationIntervalMinutes: 240));
    }

    private static CalendarEvent CreateEvent() => CalendarEvent.Create(
        Guid.NewGuid(),
        "Meeting",
        new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero),
        "UTC");
}
