namespace EventDesk.Infrastructure.Persistence;

public sealed class ReminderScheduleCursor
{
    public int Id { get; set; } = 1;
    public DateTimeOffset ThroughUtc { get; set; }
}
