using System.IO.Pipes;
using System.Text.Json;
using EventDesk.Constants;
using EventDesk.Contracts.Reminders;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.Ipc.Server;

public sealed class NamedPipeDesktopReminderServer : IDesktopReminderServer
{
    private readonly string pipeName;

    public NamedPipeDesktopReminderServer() : this(IpcConstants.ReminderPipeName) { }

    public NamedPipeDesktopReminderServer(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        this.pipeName = pipeName;
    }

    public async Task RunAsync(Func<DesktopReminderMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using NamedPipeServerStream pipe = new(pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipe.WaitForConnectionAsync(cancellationToken);
            try
            {
                using StreamReader reader = new(pipe, leaveOpen: true);
                using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
                string? line = await reader.ReadLineAsync(cancellationToken);
                DesktopReminderMessage? message = line is null ? null :
                    JsonSerializer.Deserialize<DesktopReminderMessage>(line);
                if (message is null || message.DeliveryId == Guid.Empty ||
                    message.EventId == Guid.Empty || string.IsNullOrWhiteSpace(message.EventTitle))
                    continue;

                await handler(message, cancellationToken);
                await writer.WriteLineAsync(JsonSerializer.Serialize(
                    new DesktopReminderAcknowledgement(message.DeliveryId)).AsMemory(), cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // The Service receives no acknowledgement and retries the same delivery ID.
            }
        }
    }
}
