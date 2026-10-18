# Event model and time rules

`CalendarEvent` is the aggregate root for an event and its reminder rules. A new event is enabled. Disabling it suppresses its reminder schedule; enabling it restores the schedule. `Delete()` soft-deletes the event, disables it, preserves its rules for future history references, and prevents later edits or re-enabling. A reminder rule selects exactly one delivery channel: desktop or email. To notify through both channels, add two rules.

## Event times

- `Create` and `Reschedule` accept local `DateTimeOffset` values **with explicit offsets** and a system time zone ID, such as `Europe/Prague`. Both offsets must match that zone at their respective instants.
- The event stores the start and end as UTC instants and retains the time zone ID for display. The end must be strictly later than the start when compared as an instant, even when a daylight-saving transition makes local clock readings appear out of order.
- A local time skipped by the spring transition is rejected: neither offset identifies that wall time in the zone.
- A local time repeated by the autumn transition is accepted with either valid offset. The chosen offset identifies which occurrence the user meant. A caller must supply that offset explicitly; the domain never guesses.
- Rescheduling supplies both endpoints and a zone again. Validation completes before the stored schedule changes.
- If system time zone rules change later, the stored UTC instants remain fixed. The retained zone ID is used with the current system rules when displaying local time.

## Calendar recurrence

- `None` is a single event. `Yearly` stores a month and day, without a meaningful year. `Monthly` stores only a day of month. The UTC start and end of a recurring event serve as anchors for its local notification times and time zone; their date is not a recurrence cutoff.
- A missing date is skipped. February 29 occurs only in leap years; day 31 is skipped in months with fewer than 31 days. No occurrence is shifted to another day.
- A recurring event has a first and last notification time on the same local date and a positive interval. The last time must align with the interval, so 11:00–17:00 with a three-hour interval produces 11:00, 14:00, and 17:00.
- Future local times are resolved using the stored time zone and current system rules. A time missing during a daylight-saving transition is skipped. An ambiguous time uses the earlier occurrence (the larger UTC offset).
- Calendar period queries expand a recurring event into dated occurrences. Each occurrence retains the series ID so editing or deleting an occurrence changes the whole series. Disabling a series suppresses its notification times but leaves it visible in the calendar.
- Notification delivery and delivery history are separate work; the recurrence model calculates due times but the service does not yet deliver them.

## Reminder times

- `LeadTime` is a nonnegative elapsed duration before the event starts. Zero means at the start.
- `OccurrenceCount` includes the first notification. One occurrence has no repeat interval. Multiple occurrences require a positive interval, and the last occurrence must be no later than the event start.
- Trigger times are calculated in UTC using elapsed durations. Daylight-saving transitions do not duplicate or skip reminders.
- A rule cannot be attached if its first trigger would precede the supported `DateTimeOffset` range. Repeating or rescheduling an event cannot invalidate an attached rule.
- Identical rules cannot be added twice. Each trigger records its channel and UTC due time; delivery state and history belong to later application and persistence work.
