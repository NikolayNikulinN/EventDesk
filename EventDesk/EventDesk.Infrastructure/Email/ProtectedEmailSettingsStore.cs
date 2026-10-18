using System.Security.Cryptography;
using System.Text.Json;
using EventDesk.Application.Email;
using EventDesk.Constants;

namespace EventDesk.Infrastructure.Email;

public sealed class ProtectedEmailSettingsStore(string path) : IEmailSettingsStore
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<EmailSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveAsync(EmailSettings settings, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        EmailSettingsValidator.Validate(settings);
        await gate.WaitAsync(cancellationToken);
        try
        {
            string directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);
            string temporaryPath = path + Guid.NewGuid().ToString("N") + EmailConstants.SettingsFileExtension;
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(settings);
            try
            {
                byte[] protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                try
                {
                    await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
                    File.Move(temporaryPath, path, true);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(protectedBytes);
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<EmailSettings> ReadAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!File.Exists(path)) return EmailSettings.Empty;
        byte[] protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            try
            {
                EmailSettings settings = JsonSerializer.Deserialize<EmailSettings>(plain)
                    ?? throw new InvalidDataException(EmailConstants.InvalidSettings);
                if (settings.RecipientAddresses is not null) return settings;

                using JsonDocument legacy = JsonDocument.Parse(plain);
                string? oldAddress = legacy.RootElement.TryGetProperty(EmailConstants.LegacyRecipientAddressProperty, out JsonElement value)
                    ? value.GetString() : null;
                return settings with
                {
                    RecipientAddresses = string.IsNullOrWhiteSpace(oldAddress) ? [] : [oldAddress]
                };
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }
}
