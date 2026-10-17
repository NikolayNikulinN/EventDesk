using EventDesk.Domain.Reminders;

namespace EventDesk.Application.Events.Models;

public sealed record ReminderRuleModel(
                                        TimeSpan LeadTime,
                                        ReminderChannel Channel,
                                        int OccurrenceCount,
                                        TimeSpan? RepeatInterval);
