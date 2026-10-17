namespace EventDesk.Contracts.Ipc;

public enum IpcErrorCode
{
    None = 0,
    InvalidRequest = 1,
    NotFound = 2,
    TimedOut = 3,
    InternalError = 4,
    TransportError = 5,
    AccessDenied = 6
}
