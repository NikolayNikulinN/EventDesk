using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using EventDesk.Application.Email;
using EventDesk.Infrastructure.Email;
using Xunit;

namespace EventDesk.Infrastructure.Tests.Email;

public sealed class ProtectedEmailSettingsStoreTests
{
    [Fact]
    public async Task ReadsExistingSingleRecipientSettings()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string directory = Path.Combine(Path.GetTempPath(), "EventDeskTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "MailSettings.dat");
        try
        {
            Directory.CreateDirectory(directory);
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(new
            {
                IsEnabled = true,
                Host = "smtp.example.com",
                Port = 587,
                UseImplicitTls = false,
                UserName = "user@example.com",
                FromAddress = "user@example.com",
                RecipientAddress = "previous@example.com",
                Password = "old-secret"
            });
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(path, encrypted, cancellationToken);
            EmailSettings settings = await new ProtectedEmailSettingsStore(path).GetAsync(cancellationToken);
            Assert.Equal(["previous@example.com"], settings.RecipientAddresses);
            Assert.Equal("old-secret", settings.Password);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SettingsAreEncryptedAndReadableAfterStoreRecreation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string directory = Path.Combine(Path.GetTempPath(), "EventDeskTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "MailSettings.dat");
        try
        {
            EmailSettings settings = new(true, "smtp.example.com", 587, false,
                "user@example.com", "user@example.com",
                ["recipient@example.com", "second@example.com"], "unique-test-secret");
            await new ProtectedEmailSettingsStore(path).SaveAsync(settings, cancellationToken);

            string stored = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path, cancellationToken));
            Assert.DoesNotContain(settings.Password, stored);
            Assert.DoesNotContain(settings.Host, stored);

            EmailSettings reloaded = await new ProtectedEmailSettingsStore(path).GetAsync(cancellationToken);
            Assert.Equal(settings.IsEnabled, reloaded.IsEnabled);
            Assert.Equal(settings.Host, reloaded.Host);
            Assert.Equal(settings.Password, reloaded.Password);
            Assert.Equal(settings.RecipientAddresses, reloaded.RecipientAddresses);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
