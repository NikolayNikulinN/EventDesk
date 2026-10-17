using System.IO.Pipes;
using System.Text.Json;
using EventDesk.Constants;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc.Abstractions;
using EventDesk.Ipc.Serialization;

namespace EventDesk.Ipc.Client;

public sealed class NamedPipeIpcClient : IIpcClient
{
    private readonly string pipeName;
    private readonly TimeSpan connectTimeout;
    private readonly TimeSpan requestTimeout;

    public NamedPipeIpcClient(
        string pipeName,
        TimeSpan? connectTimeout = null,
        TimeSpan? requestTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        this.pipeName = pipeName;
        this.connectTimeout = connectTimeout ?? IpcConstants.ConnectTimeout;
        this.requestTimeout = requestTimeout ?? IpcConstants.RequestTimeout;
        if (this.connectTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(connectTimeout));
        }

        if (this.requestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        IpcOperation operation,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guid correlationId = Guid.NewGuid();
        IpcRequest request = new(correlationId, operation, IpcJson.ToPayload(payload));
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout);

        try
        {
            await using NamedPipeClientStream pipe = new(
                IpcConstants.LocalServerName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            using (CancellationTokenSource connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token))
            {
                connectTimeout.CancelAfter(this.connectTimeout);
                try
                {
                    await pipe.ConnectAsync(connectTimeout.Token);
                }
                catch (OperationCanceledException exception) when (!timeout.IsCancellationRequested)
                {
                    throw new IpcException(IpcErrorCode.TransportError, IpcConstants.PipeUnavailable, exception);
                }
            }
            await PipeMessageSerializer.WriteAsync(pipe, request, timeout.Token);
            IpcResponse response = await PipeMessageSerializer.ReadAsync<IpcResponse>(pipe, timeout.Token);
            if (response.CorrelationId != correlationId)
            {
                throw new IpcException(IpcErrorCode.TransportError, IpcConstants.MismatchedResponseId);
            }

            if (response.ErrorCode != IpcErrorCode.None)
            {
                throw new IpcException(response.ErrorCode, GetErrorMessage(response.ErrorCode));
            }

            if (response.Payload is not { } responsePayload)
            {
                throw new IpcException(IpcErrorCode.TransportError, IpcConstants.MissingResponsePayload);
            }

            return IpcJson.FromPayload<TResponse>(responsePayload);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IpcException(IpcErrorCode.TimedOut, IpcConstants.PipeRequestTimedOut, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new IpcException(IpcErrorCode.AccessDenied, IpcConstants.PipeAccessDenied, exception);
        }
        catch (IOException exception)
        {
            throw new IpcException(IpcErrorCode.TransportError, IpcConstants.PipeUnavailable, exception);
        }
        catch (TimeoutException exception)
        {
            throw new IpcException(IpcErrorCode.TimedOut, IpcConstants.PipeRequestTimedOut, exception);
        }
        catch (JsonException exception)
        {
            throw new IpcException(IpcErrorCode.TransportError, IpcConstants.InvalidPipeMessage, exception);
        }
        catch (InvalidDataException exception)
        {
            throw new IpcException(IpcErrorCode.TransportError, IpcConstants.InvalidPipeMessage, exception);
        }
    }

    private static string GetErrorMessage(IpcErrorCode errorCode) => errorCode switch
    {
        IpcErrorCode.InvalidRequest => IpcConstants.InvalidRequest,
        IpcErrorCode.NotFound => IpcConstants.EventNotFound,
        IpcErrorCode.TimedOut => IpcConstants.PipeRequestTimedOut,
        _ => IpcConstants.ServiceFailed
    };
}
