namespace EventDesk.Constants;

public static class IpcConstants
{
    public const string PipeName = "EventDesk.Pipe";
    public const string ReminderPipeName = "EventDesk.Reminders";
    public const string LocalServerName = ".";
    public const string EmptyPipeMessage = "The pipe message was empty.";
    public const string InvalidPipeMessageLength = "The pipe message length is invalid.";
    public const string InvalidPipeMessage = "The pipe message is invalid.";
    public const string MissingResponsePayload = "The pipe response has no payload.";
    public const string MismatchedResponseId = "The pipe response correlation ID does not match the request.";
    public const string PipeUnavailable = "The EventDesk service pipe is unavailable.";
    public const string PipeAccessDenied = "Access to the EventDesk service pipe was denied.";
    public const string PipeRequestTimedOut = "The EventDesk service request timed out.";
    public const string PipeRequestFailed = "The EventDesk service request failed.";
    public const string InvalidRequest = "The EventDesk service rejected an invalid request.";
    public const string EventNotFound = "The requested calendar event was not found.";
    public const string ServiceFailed = "The EventDesk service could not complete the request.";
    public const string IpcServerStarted = "EventDesk IPC server started";
    public const string IpcServerStopped = "EventDesk IPC server stopped";
    public const string IpcServerFailed = "EventDesk IPC server stopped unexpectedly";
    public const string IpcRequestFailedLog = "IPC request {CorrelationId} failed";
    public const string IpcRequestCanceledLog = "IPC request {CorrelationId} was canceled";
    public const int MaxMessageBytes = 1_048_576;
    public const int MaxConcurrentConnections = 8;
    public const int PipeBufferBytes = 4096;
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan ErrorWriteTimeout = TimeSpan.FromSeconds(2);
}
