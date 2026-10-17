using EventDesk.Constants;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc.Server;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventDesk.Service;

public sealed class PipeServerWorker(
    NamedPipeIpcServer server,
    IServiceScopeFactory scopeFactory,
    ILoggerWrapper logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Write(IpcConstants.IpcServerStarted);
        try
        {
            await server.RunAsync(HandleAsync, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Write(exception, LogLevel.Fatal, IpcConstants.IpcServerFailed);
            throw;
        }
        finally
        {
            logger.Write(IpcConstants.IpcServerStopped);
        }
    }

    private async Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IpcRequestDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IpcRequestDispatcher>();
            return await dispatcher.DispatchAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Write(exception, LogLevel.Error, IpcConstants.IpcRequestFailedLog, request.CorrelationId);
            throw;
        }
    }
}
