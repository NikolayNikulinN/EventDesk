namespace EventDesk.Contracts.Email;

public sealed record SaveEmailSettingsRequest(
    bool IsEnabled, string Host, int Port, bool UseImplicitTls,
    string UserName, string FromAddress, IReadOnlyList<string> RecipientAddresses,
    string? Password);
