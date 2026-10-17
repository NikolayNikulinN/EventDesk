namespace EventDesk.Application.Events.Models;

public sealed record CalendarEventModel(
                                        Guid Id,
                                        string Title,
                                        DateTimeOffset StartsAtUtc,
                                        DateTimeOffset EndsAtUtc,
                                        string TimeZoneId,
                                        bool IsEnabled,
                                        IReadOnlyList<ReminderRuleModel> Reminders);
