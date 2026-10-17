using EventDesk.Contracts.Ipc;

namespace EventDesk.Ipc.Abstractions;

public interface IIpcClient
{
    Task<TResponse> SendAsync<TRequest, TResponse>(
        IpcOperation operation,
        TRequest payload,
        CancellationToken cancellationToken = default);
}
