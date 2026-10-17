using System.Text.Json;
using EventDesk.Constants;

namespace EventDesk.Ipc.Serialization;

public static class IpcJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 32
    };

    public static JsonElement ToPayload<T>(T value) =>
        JsonSerializer.SerializeToElement(value, Options);

    public static T FromPayload<T>(JsonElement payload) =>
        payload.Deserialize<T>(Options)
        ?? throw new JsonException(IpcConstants.InvalidPipeMessage);
}
