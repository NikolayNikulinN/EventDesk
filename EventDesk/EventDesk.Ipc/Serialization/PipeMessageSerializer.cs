using System.Buffers.Binary;
using System.Text.Json;
using EventDesk.Constants;

namespace EventDesk.Ipc.Serialization;

internal static class PipeMessageSerializer
{
    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, IpcJson.Options);
        if (payload.Length is 0 or > IpcConstants.MaxMessageBytes)
        {
            throw new InvalidDataException(IpcConstants.InvalidPipeMessageLength);
        }

        byte[] length = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(length, payload.Length);
        await stream.WriteAsync(length, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        byte[] length = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(length, cancellationToken);
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(length);
        if (payloadLength is <= 0 or > IpcConstants.MaxMessageBytes)
        {
            throw new InvalidDataException(IpcConstants.InvalidPipeMessageLength);
        }

        byte[] payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload, cancellationToken);
        return JsonSerializer.Deserialize<T>(payload, IpcJson.Options)
            ?? throw new JsonException(IpcConstants.EmptyPipeMessage);
    }
}
