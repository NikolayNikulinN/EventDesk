using EventDesk.Domain.Reminders;
using Xunit;

namespace EventDesk.Domain.Tests.Reminders;

public sealed class ReminderRuleTests
{
    [Theory]
    [InlineData(ReminderChannel.Desktop)]
    [InlineData(ReminderChannel.Email)]
    public void Create_SupportedChannel_AcceptsIt(ReminderChannel channel)
    {
        ReminderRule reminder = ReminderRule.Create(TimeSpan.Zero, channel);

        Assert.Equal(channel, reminder.Channel);
    }

    [Fact]
    public void Create_NegativeLeadTime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReminderRule.Create(
            TimeSpan.FromTicks(-1), ReminderChannel.Desktop));
    }

    [Fact]
    public void Create_UnknownChannel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(5), (ReminderChannel)99));
    }

    [Fact]
    public void Create_ZeroOccurrenceCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(5), ReminderChannel.Desktop, occurrenceCount: 0));
    }

    [Fact]
    public void Create_RepeatedWithoutPositiveInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(10), ReminderChannel.Desktop, occurrenceCount: 2));
        Assert.Throws<ArgumentException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(10), ReminderChannel.Desktop, occurrenceCount: 2, repeatInterval: TimeSpan.Zero));
    }

    [Fact]
    public void Create_OneTimeWithRepeatInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(10), ReminderChannel.Desktop, repeatInterval: TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Create_LastRepeatAfterEventStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => ReminderRule.Create(
            TimeSpan.FromMinutes(10), ReminderChannel.Desktop, occurrenceCount: 3,
            repeatInterval: TimeSpan.FromMinutes(6)));
    }

    [Fact]
    public void GetTriggerTimes_ValidRepeatedRule_IncludesEventStartAtBoundary()
    {
        ReminderRule reminder = ReminderRule.Create(
            TimeSpan.FromMinutes(10), ReminderChannel.Desktop, occurrenceCount: 3,
            repeatInterval: TimeSpan.FromMinutes(5));
        DateTimeOffset eventStart = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

        DateTimeOffset[] triggers = reminder.GetTriggerTimes(eventStart).ToArray();

        Assert.Equal(
            [eventStart.AddMinutes(-10), eventStart.AddMinutes(-5), eventStart],
            triggers);
    }
}
