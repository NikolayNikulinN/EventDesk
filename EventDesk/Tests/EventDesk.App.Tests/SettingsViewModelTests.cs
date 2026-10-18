using EventDesk.App.Services;
using EventDesk.App.ViewModels.Pages;
using EventDesk.Contracts.Email;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Resources.Wrappers;
using Xunit;

namespace EventDesk.App.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task RequiredFieldsAndRecipientAreValidatedBeforeSaving()
    {
        ResourceWrapper resources = new();
        FakeEmailSettingsClient client = new(new EmailSettingsResponse(
            false, string.Empty, 587, false, string.Empty, string.Empty, [], false));
        SettingsViewModel viewModel = new(client, resources, new SilentLogger());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsHostWatermarkVisible);
        Assert.True(viewModel.IsUserNameWatermarkVisible);
        Assert.True(viewModel.IsFromAddressWatermarkVisible);
        Assert.True(viewModel.IsPasswordWatermarkVisible);
        viewModel.IsEnabled = true;
        Assert.Equal(resources.EmailHostRequired, viewModel.HostError);
        Assert.Equal(resources.EmailRecipientsRequired, viewModel.RecipientsError);
        Assert.False(viewModel.CanSaveSettings);

        viewModel.Host = "smtp.example.com";
        Assert.False(viewModel.IsHostWatermarkVisible);
        viewModel.Port = "25";
        Assert.Equal(resources.EmailPortInvalidStartTls, viewModel.PortError);
        viewModel.Port = "587";
        viewModel.UserName = "user@example.com";
        Assert.False(viewModel.IsUserNameWatermarkVisible);
        viewModel.FromAddress = "invalid";
        Assert.Equal(resources.EmailFromAddressInvalid, viewModel.FromAddressError);
        viewModel.FromAddress = "sender@example.com";
        Assert.False(viewModel.IsFromAddressWatermarkVisible);
        Assert.Equal(resources.EmailPasswordRequired, viewModel.PasswordError);
        viewModel.SetPasswordState(11, true);
        Assert.False(viewModel.IsPasswordWatermarkVisible);

        viewModel.OpenAddRecipientCommand.Execute(null);
        Assert.True(viewModel.IsRecipientWatermarkVisible);
        viewModel.RecipientDraft = "invalid";
        Assert.Equal(resources.EmailRecipientInvalid, viewModel.RecipientDraftError);
        Assert.False(viewModel.CanSaveRecipient);
        viewModel.RecipientDraft = "person@example.com";
        Assert.False(viewModel.IsRecipientWatermarkVisible);
        viewModel.SaveRecipientCommand.Execute(null);
        Assert.True(viewModel.CanSaveSettings);

        Assert.True(await viewModel.SaveAsync("app-password", TestContext.Current.CancellationToken));
        Assert.Equal(["person@example.com"], client.LastSave!.RecipientAddresses);
    }

    [Fact]
    public async Task ExistingPasswordAllowsRecipientEditsButServerChangeRequiresItAgain()
    {
        ResourceWrapper resources = new();
        FakeEmailSettingsClient client = new(new EmailSettingsResponse(
            true, "smtp.example.com", 587, false, "user@example.com",
            "sender@example.com", ["one@example.com"], true));
        SettingsViewModel viewModel = new(client, resources, new SilentLogger());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.OpenEditRecipientCommand.Execute("one@example.com");
        viewModel.RecipientDraft = "two@example.com";
        viewModel.SaveRecipientCommand.Execute(null);
        Assert.True(viewModel.CanSaveSettings);

        viewModel.OpenAddRecipientCommand.Execute(null);
        viewModel.RecipientDraft = "two@example.com";
        Assert.Equal(resources.EmailRecipientDuplicate, viewModel.RecipientDraftError);
        viewModel.CancelRecipientCommand.Execute(null);

        viewModel.Host = "smtp.other.example.com";
        Assert.Equal(resources.EmailPasswordReenter, viewModel.PasswordError);
        Assert.False(viewModel.CanSaveSettings);
        viewModel.SetPasswordState(3, false);
        Assert.Equal(resources.EmailPasswordRequired, viewModel.PasswordError);
        viewModel.SetPasswordState(11, true);
        Assert.True(viewModel.CanSaveSettings);
    }

    private sealed class FakeEmailSettingsClient(EmailSettingsResponse settings) : IEmailSettingsClient
    {
        public SaveEmailSettingsRequest? LastSave { get; private set; }

        public Task<EmailSettingsResponse> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);

        public Task<EmailSettingsResponse> SaveAsync(
            SaveEmailSettingsRequest request, CancellationToken cancellationToken = default)
        {
            LastSave = request;
            settings = new EmailSettingsResponse(
                request.IsEnabled, request.Host, request.Port, request.UseImplicitTls,
                request.UserName, request.FromAddress, request.RecipientAddresses, true);
            return Task.FromResult(settings);
        }
    }

    private sealed class SilentLogger : ILoggerWrapper
    {
        public void Write(string message, params object?[] propertyValues) { }
        public void Write(string message, LogLevel level) { }
        public void Write(Exception? exception, LogLevel level, string messageTemplate,
            params object?[] propertyValues) { }
        public void CloseAndFlush() { }
    }
}
