namespace EventDesk.Contracts.Events;

public sealed record UpdateEventRequest(Guid Id, SaveEventDto Event);
