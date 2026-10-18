using EventDesk.Contracts.Email;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.App.Services;

public sealed class EmailSettingsIpcClient(IIpcClient ipcClient) : IEmailSettingsClient
{
    public Task<EmailSettingsResponse> GetAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetEmailSettingsRequest, EmailSettingsResponse>(
            IpcOperation.GetEmailSettings, new GetEmailSettingsRequest(), cancellationToken);

    public Task<EmailSettingsResponse> SaveAsync(SaveEmailSettingsRequest settings,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SaveEmailSettingsRequest, EmailSettingsResponse>(
            IpcOperation.SaveEmailSettings, settings, cancellationToken);
}
