using EventDesk.Contracts.Ipc;

namespace EventDesk.Ipc;

public sealed class IpcException : Exception
{
    public IpcException(IpcErrorCode errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public IpcErrorCode ErrorCode { get; }
}
