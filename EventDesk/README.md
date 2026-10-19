# EventDesk

EventDesk is a Windows desktop calendar and reminder application. It includes a weekly calendar UI, event persistence in SQLite, named-pipe communication between the WPF app and the service, and desktop and email reminder delivery.

For installation and everyday use, see the [user guide](Docs/user-guide.md).

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

The desktop shell opens Calendar, History, and Settings through WPF-UI navigation. Calendar provides a mini-calendar, a weekly time grid, and an event editor. Select a date, create an event, select an event card to edit or disable it, and delete it through the editor. One-time events can request a desktop reminder, an email reminder, or both. History shows the channel, attempts, status, and last delivery error.

## Notifications and email

Closing the main window keeps EventDesk running in the tray. The service sends due desktop reminders to the app through a named pipe; the app shows a Windows tray notification. If the app is not running, the service retries delivery. Use the tray menu to reopen or exit the app.

To send email reminders, open Settings and enter the SMTP host, account, sender, and recipient email addresses. Use STARTTLS on port 587 or implicit TLS on port 465. Enable email delivery and save. Recipient additions, edits, and deletions take effect when the email settings are saved. The password is sent only to the local service when saved, and Settings never reads it back. The service encrypts the entire mail configuration with Windows DPAPI under its own account in `%ProgramData%\EventDesk\Data\MailSettings.dat`. Keep the service account the same after configuration; a different account cannot decrypt the file. Changing the server, account, or sender requires the password again. Messages are sent with recipients in Bcc.

History marks email as delivered when the SMTP server accepts the message. Final arrival in the recipient's inbox is outside the app's control. SMTP has no general delivery idempotency guarantee, so a service crash after SMTP acceptance but before History is updated can result in a repeated email. Recurring event notifications currently use the desktop channel.

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

## Installer

The x64 WiX bundle publishes both executables as self-contained .NET 9 applications, then packages them in a per-machine MSI. Build it on Windows with the .NET 9 SDK:

```powershell
dotnet build EventDesk.Installer/EventDesk.Installer.wixproj -c Release -p:Platform=x64 -p:EventDeskVersion=0.2.0
```

The output is under `EventDesk.Installer/bin/Release/x64/`. Increment `EventDeskVersion` (the first three numeric fields) for every release so the new MSI can replace the previous one. The bundle and MSI use the same version property. WiX Toolset 7 requires the builder to review its [OSMF terms and EULA](https://docs.firegiant.com/wix/osmf/) and accept them locally before building. This repository does not accept the agreement on a builder's behalf.

The installer registers `EventDesk.Service` to start automatically, creates Start menu and desktop shortcuts, and starts the desktop app in the tray at user logon. It leaves `%ProgramData%\EventDesk\Data` in place on upgrade or removal. The service creates and migrates the SQLite database at startup. See [release verification](Docs/release-verification.md) for clean install, upgrade, and removal checks.
