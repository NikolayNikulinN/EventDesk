# First IPC scenario

The desktop calendar page requests events for the current local week when it
opens and when the user selects Refresh. The WPF process calls
`CalendarEventIpcClient`; only the Windows Service opens SQLite.

```text
CalendarViewModel -> NamedPipeIpcClient -> EventDesk.Pipe
    -> PipeServerWorker -> one DI scope -> IpcRequestDispatcher
    -> CalendarEventService -> repository / Unit of Work -> SQLite
```

The service accepts GetEvents, CreateEvent, UpdateEvent and DeleteEvent. Each
request uses a fresh pipe connection and a correlation ID. Messages contain a
four-byte little-endian length followed by UTF-8 JSON. The maximum JSON
message size is 1 MiB; at most eight connections are processed concurrently.
The client waits up to two seconds to connect and up to fifteen seconds for
the whole request. The server also cancels each request after fifteen seconds,
when the client disconnects, or when the service shuts down.

Responses contain a payload or a typed error code. Invalid requests, missing
events, timeouts and internal failures are distinguished without sending
exception details to the desktop process. The WPF page displays localized
status text from `EventDesk.Resources`.

On Windows the service pipe ACL gives the service identity, SYSTEM and
Administrators full control. Interactive users can read and write the pipe but
cannot create new pipe instances. The current database is machine-wide, so
all interactive users who can connect to the pipe can access its calendar
events. Per-user separation needs an explicit ownership model before this is
suitable for a multi-user installation.

`Tests/EventDesk.Ipc.Tests` exercises the request path through the actual
pipe, service dispatcher and SQLite, including a disconnected client,
separate request scopes, domain errors, request timeout and cancellation.
