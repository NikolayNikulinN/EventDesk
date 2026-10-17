using System.Text.Json;

namespace EventDesk.Contracts.Ipc;

public sealed record IpcResponse(Guid CorrelationId, IpcErrorCode ErrorCode, JsonElement? Payload);
