using System.IO.Pipes;
using System.Text.Json;
using EventDesk.Constants;
using EventDesk.Contracts.Reminders;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.Ipc.Client;

public sealed class NamedPipeDesktopReminderClient : IDesktopReminderClient
{
    private readonly string pipeName;

    public NamedPipeDesktopReminderClient() : this(IpcConstants.ReminderPipeName) { }

    public NamedPipeDesktopReminderClient(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        this.pipeName = pipeName;
    }

    public async Task SendAsync(DesktopReminderMessage message, CancellationToken cancellationToken)
    {
        await using NamedPipeClientStream pipe = new(IpcConstants.LocalServerName,
            pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(1000, cancellationToken);
        using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        using StreamReader reader = new(pipe, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), cancellationToken);
        string? line = await reader.ReadLineAsync(cancellationToken);
        DesktopReminderAcknowledgement? acknowledgement = line is null ? null :
            JsonSerializer.Deserialize<DesktopReminderAcknowledgement>(line);
        if (acknowledgement?.DeliveryId != message.DeliveryId)
            throw new IOException(ReminderProcessingConstants.InvalidAcknowledgement);
    }
}
