using System.Text.Json;

namespace EventDesk.Contracts.Ipc;

public sealed record IpcRequest(Guid CorrelationId, IpcOperation Operation, JsonElement Payload);
