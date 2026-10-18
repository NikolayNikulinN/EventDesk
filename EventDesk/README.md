# EventDesk

EventDesk is a Windows desktop calendar and reminder application. The current implementation includes a weekly calendar UI, event persistence in SQLite, and named-pipe communication between the WPF app and the service. Notification delivery and the installer payload are planned for later tasks.

The event and reminder invariants, including daylight-saving rules, are documented in [the event model](Docs/event-model.md).

## Projects

- `EventDesk.Domain` — business model and rules.
- `EventDesk.Application` — use cases and ports.
- `EventDesk.Infrastructure` — persistence and external integrations.
- `EventDesk.Contracts` — messages shared by the two processes.
- `EventDesk.Ipc` — named-pipe transport.
- `EventDesk.App` — WPF application (`EventDesk.exe`).
- `EventDesk.Service` — Windows Service process.
- `EventDesk.Installer` — WiX installer project.
- `EventDesk.Resources` — UI strings, styles, images, and controls.
- `EventDesk.Constants` — shared technical identifiers.
- `EventDesk.Logger` — application logging abstraction and Serilog implementation.
- `Tests/*` — xUnit test projects for the core layers.

The desktop application references Contracts, IPC, Resources, and Logger. The service is the composition root for Application, Infrastructure, Contracts, IPC, Constants, and Logger. Domain and Application remain independent of WPF and EF Core.

The desktop shell opens Calendar, History, Settings, and About pages through WPF-UI navigation. Calendar provides a mini-calendar, a weekly time grid, and an event editor. Select a date, create an event, select an event card to edit or disable it, and delete it through the editor. History, Settings, and About are placeholders.

## Build

Requires the .NET 9 SDK on Windows.

```powershell
dotnet build EventDesk.sln
dotnet test EventDesk.sln
```

For local use, start the service and then the desktop app in separate terminals:

```powershell
dotnet run --project EventDesk.Service
dotnet run --project EventDesk.App
```

The service owns the database under `%ProgramData%\EventDesk\Data\EventDesk.db`; the desktop app only communicates through the named pipe. The service account needs write access to that directory.

The installer is a placeholder package. It does not yet install or configure the application or service.
WiX Toolset 7 requires its EULA to be accepted by the developer before the installer project can be built. The WiX project is included in the solution but is not part of the default solution build configuration.
