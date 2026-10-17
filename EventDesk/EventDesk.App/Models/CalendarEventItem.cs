namespace EventDesk.App.Models;

public sealed record CalendarEventItem(Guid Id, string Title, DateTimeOffset StartsAtLocal);
