namespace EventDesk.Contracts.Email;

public sealed record EmailSettingsResponse(
    bool IsEnabled, string Host, int Port, bool UseImplicitTls,
    string UserName, string FromAddress, IReadOnlyList<string> RecipientAddresses,
    bool HasPassword);
