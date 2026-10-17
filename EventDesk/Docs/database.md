# EventDesk database

The initial SQLite schema has two application tables:

| Table | Purpose | Key fields |
| --- | --- | --- |
| `CalendarEvents` | Events, including disabled and soft-deleted events | `Id` (GUID primary key), `Title`, `StartsAtUtcTicks`, `EndsAtUtcTicks`, `TimeZoneId`, `IsEnabled`, `IsDeleted` |
| `ReminderRules` | Reminder schedules attached to events | `Id` (GUID primary key), `EventId` (foreign key), `LeadTimeTicks`, `Channel`, `OccurrenceCount`, `RepeatIntervalTicks` |

One event has zero or more reminder rules. `ReminderRules.EventId` references
`CalendarEvents.Id`. Deleting an event through the application marks it as
deleted and keeps its rules for future history. A physical database deletion
cascades to reminder rules. Indexes on the deletion flag and event boundaries
support period queries. A period query uses half-open intervals: an event is
returned when `StartsAtUtc < periodEnd` and `EndsAtUtc > periodStart`.

EF Core also creates `__EFMigrationsHistory` and its migration lock table.
Notification delivery history is deferred until reminder delivery is added.

The tracked template is `DataBase/EventDesk.db`. The per-machine installer
places it at `%ProgramData%\EventDesk\Data\EventDesk.db` (normally
`C:\ProgramData\EventDesk\Data\EventDesk.db`). It preserves an existing
database during upgrades and uninstall. Installer ACLs give `SYSTEM` and
Administrators full access to the data directory and database file; inherited
permissions also protect SQLite WAL sidecar files. The service is the only
application process that opens this database and must run as `LocalSystem` with
this installer configuration.

Service startup applies any pending EF Core migrations and confirms that
SQLite journal mode is WAL. All event instants are stored as UTC ticks; the
original time zone ID is retained for display. The desktop application uses
IPC and does not access the database directly.
