using System.Net.Mail;
using EventDesk.Constants;

namespace EventDesk.Application.Email;

public static class EmailSettingsValidator
{
    public static void Validate(EmailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Host is null || settings.UserName is null ||
            settings.FromAddress is null || settings.RecipientAddresses is null ||
            settings.Password is null || settings.Host.Length > 253 ||
            (!string.IsNullOrWhiteSpace(settings.Host) && Uri.CheckHostName(settings.Host) == UriHostNameType.Unknown) ||
            settings.Port != (settings.UseImplicitTls ? EmailConstants.ImplicitTlsPort : EmailConstants.StartTlsPort) ||
            settings.UserName.Length > 320 || settings.Password.Length > 1024 ||
            !IsAddressOrEmpty(settings.FromAddress) ||
            settings.RecipientAddresses.Count > EmailConstants.MaximumRecipients ||
            settings.RecipientAddresses.Any(address => !IsValidAddress(address)) ||
            settings.RecipientAddresses.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                settings.RecipientAddresses.Count ||
            (settings.IsEnabled &&
             (string.IsNullOrWhiteSpace(settings.Host) ||
              string.IsNullOrWhiteSpace(settings.FromAddress) ||
              settings.RecipientAddresses.Count == 0 ||
              string.IsNullOrWhiteSpace(settings.UserName) ||
              string.IsNullOrWhiteSpace(settings.Password))))
        {
            throw new ArgumentException(EmailConstants.InvalidSettings, nameof(settings));
        }
    }

    private static bool IsAddressOrEmpty(string address) =>
        string.IsNullOrWhiteSpace(address) ||
        IsValidAddress(address);

    private static bool IsValidAddress(string? address) =>
        !string.IsNullOrWhiteSpace(address) && address.Length <= 320 &&
        MailAddress.TryCreate(address, out MailAddress? parsed) &&
        string.Equals(parsed.Address, address, StringComparison.Ordinal);
}
