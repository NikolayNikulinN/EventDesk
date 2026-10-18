namespace EventDesk.Application.Email;

public interface IEmailSettingsStore
{
    Task<EmailSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(EmailSettings settings, CancellationToken cancellationToken = default);
}
