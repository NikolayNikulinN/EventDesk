using System.IO.Pipes;
using System.Text.Json;
using EventDesk.Constants;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc.Serialization;

namespace EventDesk.Ipc.Server;

public sealed class NamedPipeIpcServer(string pipeName, Func<NamedPipeServerStream>? streamFactory = null)
{
    public async Task RunAsync(
        Func<IpcRequest, CancellationToken, Task<IpcResponse>> handler,
        CancellationToken stoppingToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(handler);
        List<Task> active = [];

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                for (int index = active.Count - 1; index >= 0; index--)
                {
                    if (active[index].IsCompleted)
                    {
                        await active[index];
                        active.RemoveAt(index);
                    }
                }
                if (active.Count >= IpcConstants.MaxConcurrentConnections)
                {
                    await Task.WhenAny(active);
                    continue;
                }

                NamedPipeServerStream pipe = streamFactory?.Invoke() ?? new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    IpcConstants.MaxConcurrentConnections,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    IpcConstants.PipeBufferBytes,
                    IpcConstants.PipeBufferBytes);
                try
                {
                    await pipe.WaitForConnectionAsync(stoppingToken);
                }
                catch
                {
                    await pipe.DisposeAsync();
                    throw;
                }

                active.Add(HandleConnectionAsync(pipe, handler, stoppingToken));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await Task.WhenAll(active);
        }
    }

    private static async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        Func<IpcRequest, CancellationToken, Task<IpcResponse>> handler,
        CancellationToken stoppingToken)
    {
        await using (pipe)
        {
            Guid correlationId = Guid.Empty;
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(IpcConstants.RequestTimeout);
            try
            {
                IpcRequest request = await PipeMessageSerializer.ReadAsync<IpcRequest>(pipe, timeout.Token);
                correlationId = request.CorrelationId;
                if (correlationId == Guid.Empty || request.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    throw new InvalidDataException(IpcConstants.InvalidPipeMessage);
                }

                using CancellationTokenSource handlerCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                using CancellationTokenSource disconnectMonitor =
                    CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                Task monitorTask = MonitorClientDisconnectAsync(
                    pipe, handlerCancellation, disconnectMonitor.Token);
                IpcResponse response;
                try
                {
                    response = await handler(request, handlerCancellation.Token);
                }
                finally
                {
                    await disconnectMonitor.CancelAsync();
                    await monitorTask;
                }

                await PipeMessageSerializer.WriteAsync(pipe, response, timeout.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                await TryWriteErrorAsync(pipe, correlationId, IpcErrorCode.TimedOut, stoppingToken);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or EndOfStreamException)
            {
                await TryWriteErrorAsync(pipe, correlationId, IpcErrorCode.InvalidRequest, stoppingToken);
            }
            catch (IOException)
            {
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                await TryWriteErrorAsync(pipe, correlationId, IpcErrorCode.InternalError, stoppingToken);
            }
        }
    }

    private static async Task MonitorClientDisconnectAsync(
        NamedPipeServerStream pipe,
        CancellationTokenSource handlerCancellation,
        CancellationToken monitorToken)
    {
        try
        {
            byte[] buffer = new byte[1];
            // The protocol allows one request per connection. A close or extra input cancels its handler.
            await pipe.ReadExactlyAsync(buffer, monitorToken);
            await handlerCancellation.CancelAsync();
        }
        catch (OperationCanceledException) when (monitorToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            await handlerCancellation.CancelAsync();
        }
    }

    private static async Task TryWriteErrorAsync(
        NamedPipeServerStream pipe,
        Guid correlationId,
        IpcErrorCode errorCode,
        CancellationToken stoppingToken)
    {
        if (!pipe.IsConnected)
        {
            return;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(IpcConstants.ErrorWriteTimeout);
        try
        {
            await PipeMessageSerializer.WriteAsync(
                pipe, new IpcResponse(correlationId, errorCode, null), timeout.Token);
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException)
        {
        }
    }
}
