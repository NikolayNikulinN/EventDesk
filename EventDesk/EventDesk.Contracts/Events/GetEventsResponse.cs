namespace EventDesk.Contracts.Events;

public sealed record GetEventsResponse(IReadOnlyList<CalendarEventResponse> Events);
