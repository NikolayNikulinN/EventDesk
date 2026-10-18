using EventDesk.Contracts.Email;

namespace EventDesk.App.Services;

public interface IEmailSettingsClient
{
    Task<EmailSettingsResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<EmailSettingsResponse> SaveAsync(SaveEmailSettingsRequest settings,
        CancellationToken cancellationToken = default);
}
