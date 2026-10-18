using EventDesk.Constants;

namespace EventDesk.Application.Email;

public sealed record EmailSettings(
    bool IsEnabled, string Host, int Port, bool UseImplicitTls,
    string UserName, string FromAddress, IReadOnlyList<string> RecipientAddresses,
    string Password)
{
    public static EmailSettings Empty { get; } = new(false, string.Empty, EmailConstants.StartTlsPort, false,
        string.Empty, string.Empty, [], string.Empty);
}
