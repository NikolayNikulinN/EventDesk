namespace EventDesk.Contracts.Events;

public sealed record GetEventsRequest(DateTimeOffset From, DateTimeOffset To);
