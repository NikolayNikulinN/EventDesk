# Event model and time rules

`CalendarEvent` is the aggregate root for an event and its reminder rules. A new event is enabled. Disabling it suppresses its reminder schedule; enabling it restores the schedule. `Delete()` soft-deletes the event, disables it, preserves its rules for future history references, and prevents later edits or re-enabling. A reminder rule selects exactly one delivery channel: desktop or email. To notify through both channels, add two rules.

## Event times

- `Create` and `Reschedule` accept local `DateTimeOffset` values **with explicit offsets** and a system time zone ID, such as `Europe/Prague`. Both offsets must match that zone at their respective instants.
- The event stores the start and end as UTC instants and retains the time zone ID for display. The end must be strictly later than the start when compared as an instant, even when a daylight-saving transition makes local clock readings appear out of order.
- A local time skipped by the spring transition is rejected: neither offset identifies that wall time in the zone.
- A local time repeated by the autumn transition is accepted with either valid offset. The chosen offset identifies which occurrence the user meant. A caller must supply that offset explicitly; the domain never guesses.
- Rescheduling supplies both endpoints and a zone again. Validation completes before the stored schedule changes.
- If system time zone rules change later, the stored UTC instants remain fixed. The retained zone ID is used with the current system rules when displaying local time. A future recurring-event model will need its own rule for resolving future wall times.

## Reminder times

- `LeadTime` is a nonnegative elapsed duration before the event starts. Zero means at the start.
- `OccurrenceCount` includes the first notification. One occurrence has no repeat interval. Multiple occurrences require a positive interval, and the last occurrence must be no later than the event start.
- Trigger times are calculated in UTC using elapsed durations. Daylight-saving transitions do not duplicate or skip reminders.
- A rule cannot be attached if its first trigger would precede the supported `DateTimeOffset` range. Repeating or rescheduling an event cannot invalidate an attached rule.
- Identical rules cannot be added twice. Each trigger records its channel and UTC due time; delivery state and history belong to later application and persistence work.
