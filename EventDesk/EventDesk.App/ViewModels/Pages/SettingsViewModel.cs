using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventDesk.App.Services;
using EventDesk.Constants;
using EventDesk.Contracts.Email;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IEmailSettingsClient client;
    private readonly IResourceWrapper resources;
    private readonly ILoggerWrapper logger;
    private bool isEnabled, useImplicitTls, isBusy, hasSavedPassword, hasLoaded;
    private int newPasswordLength;
    private bool hasNonWhitespacePassword;
    private bool isRecipientEditorOpen, isDeleteConfirmationOpen;
    private string host = string.Empty;
    private string port = EmailConstants.StartTlsPort.ToString(CultureInfo.InvariantCulture);
    private string userName = string.Empty, fromAddress = string.Empty, recipientDraft = string.Empty;
    private string passwordHint = string.Empty, status = string.Empty;
    private string? editingRecipient, deletingRecipient;
    private string loadedHost = string.Empty, loadedUserName = string.Empty, loadedFromAddress = string.Empty;
    private int loadedPort = EmailConstants.StartTlsPort;
    private bool loadedUseImplicitTls;

    public SettingsViewModel(IEmailSettingsClient client, IResourceWrapper resources, ILoggerWrapper logger)
    {
        this.client = client;
        this.resources = resources;
        this.logger = logger;
        RecipientAddresses.CollectionChanged += (_, _) => Changed();
        OpenAddRecipientCommand = new RelayCommand(OpenAddRecipient);
        OpenEditRecipientCommand = new RelayCommand<string>(OpenEditRecipient);
        RequestDeleteRecipientCommand = new RelayCommand<string>(RequestDeleteRecipient);
        SaveRecipientCommand = new RelayCommand(SaveRecipient);
        CancelRecipientCommand = new RelayCommand(() => IsRecipientEditorOpen = false);
        ConfirmDeleteRecipientCommand = new RelayCommand(ConfirmDeleteRecipient);
        CancelDeleteRecipientCommand = new RelayCommand(() => IsDeleteConfirmationOpen = false);
    }

    public IResourceWrapper Resources => resources;
    public string Title => resources.Settings;
    public ObservableCollection<string> RecipientAddresses { get; } = [];
    public IRelayCommand OpenAddRecipientCommand { get; }
    public IRelayCommand<string> OpenEditRecipientCommand { get; }
    public IRelayCommand<string> RequestDeleteRecipientCommand { get; }
    public IRelayCommand SaveRecipientCommand { get; }
    public IRelayCommand CancelRecipientCommand { get; }
    public IRelayCommand ConfirmDeleteRecipientCommand { get; }
    public IRelayCommand CancelDeleteRecipientCommand { get; }
    public string Host { get => host; set { if (SetProperty(ref host, value)) Changed(); } }
    public string Port { get => port; set { if (SetProperty(ref port, value)) Changed(); } }
    public string UserName { get => userName; set { if (SetProperty(ref userName, value)) Changed(); } }
    public string FromAddress { get => fromAddress; set { if (SetProperty(ref fromAddress, value)) Changed(); } }
    public string PasswordHint { get => passwordHint; private set => SetProperty(ref passwordHint, value); }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) Changed(); } }
    public bool IsEnabled { get => isEnabled; set { if (SetProperty(ref isEnabled, value)) Changed(); } }
    public bool IsRecipientEditorOpen { get => isRecipientEditorOpen; private set => SetProperty(ref isRecipientEditorOpen, value); }
    public bool IsDeleteConfirmationOpen { get => isDeleteConfirmationOpen; private set => SetProperty(ref isDeleteConfirmationOpen, value); }
    public string RecipientDraft { get => recipientDraft; set { if (SetProperty(ref recipientDraft, value)) Changed(); } }
    public string RecipientDialogTitle => editingRecipient is null ? resources.AddRecipient : resources.EditRecipient;
    public string DeletingRecipient => deletingRecipient ?? string.Empty;
    public bool IsHostWatermarkVisible => string.IsNullOrEmpty(Host);
    public bool IsPortWatermarkVisible => string.IsNullOrEmpty(Port);
    public bool IsUserNameWatermarkVisible => string.IsNullOrEmpty(UserName);
    public bool IsPasswordWatermarkVisible => newPasswordLength == 0;
    public bool IsFromAddressWatermarkVisible => string.IsNullOrEmpty(FromAddress);
    public bool IsRecipientWatermarkVisible => string.IsNullOrEmpty(RecipientDraft);
    public string PortExample => UseImplicitTls ? resources.EmailPortExampleImplicitTls : resources.EmailPortExampleStartTls;
    public string HostError => !ShouldValidate ? string.Empty :
        string.IsNullOrWhiteSpace(Host) ? resources.EmailHostRequired :
        Host.Trim().Length > 253 || Uri.CheckHostName(Host.Trim()) == UriHostNameType.Unknown
            ? resources.EmailHostInvalid : string.Empty;
    public string PortError => !ShouldValidate ? string.Empty :
        int.TryParse(Port, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedPort) &&
        parsedPort == (UseImplicitTls ? EmailConstants.ImplicitTlsPort : EmailConstants.StartTlsPort)
            ? string.Empty
            : UseImplicitTls ? resources.EmailPortInvalidImplicitTls : resources.EmailPortInvalidStartTls;
    public string UserNameError => !ShouldValidate ? string.Empty :
        string.IsNullOrWhiteSpace(UserName) ? resources.EmailUserNameRequired :
        UserName.Trim().Length > 320 ? resources.EmailUserNameInvalid : string.Empty;
    public string PasswordError
    {
        get
        {
            if (!ShouldValidate) return string.Empty;
            if (newPasswordLength > 1024) return resources.EmailPasswordInvalid;
            if (newPasswordLength > 0)
                return hasNonWhitespacePassword ? string.Empty : resources.EmailPasswordRequired;
            if (!hasSavedPassword) return resources.EmailPasswordRequired;
            int parsedPort = int.TryParse(Port, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? value : loadedPort;
            return ConnectionChanged(parsedPort) ? resources.EmailPasswordReenter : string.Empty;
        }
    }
    public string FromAddressError => !ShouldValidate ? string.Empty :
        string.IsNullOrWhiteSpace(FromAddress) ? resources.EmailFromAddressRequired :
        !IsValidAddress(FromAddress.Trim()) ? resources.EmailFromAddressInvalid : string.Empty;
    public string RecipientsError => !ShouldValidate ? string.Empty :
        RecipientAddresses.Count == 0 ? resources.EmailRecipientsRequired :
        RecipientAddresses.Count > EmailConstants.MaximumRecipients ? resources.EmailRecipientsLimit :
        RecipientAddresses.Any(address => !IsValidAddress(address)) ? resources.EmailRecipientInvalid :
        RecipientAddresses.Distinct(StringComparer.OrdinalIgnoreCase).Count() != RecipientAddresses.Count
            ? resources.EmailRecipientDuplicate : string.Empty;
    public string RecipientDraftError
    {
        get
        {
            string address = RecipientDraft.Trim();
            if (address.Length == 0) return resources.EmailRecipientRequired;
            if (!IsValidAddress(address)) return resources.EmailRecipientInvalid;
            if (editingRecipient is null && RecipientAddresses.Count >= EmailConstants.MaximumRecipients)
                return resources.EmailRecipientsLimit;
            return RecipientAddresses.Any(existing =>
                !string.Equals(existing, editingRecipient, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing, address, StringComparison.OrdinalIgnoreCase))
                ? resources.EmailRecipientDuplicate : string.Empty;
        }
    }
    private bool ShouldValidate => hasLoaded && IsEnabled;
    public bool CanAddRecipient => hasLoaded && !IsBusy && RecipientAddresses.Count < EmailConstants.MaximumRecipients;
    public bool CanSaveRecipient => RecipientDraftError.Length == 0;
    public bool CanSaveSettings => hasLoaded && !IsBusy && (!IsEnabled ||
        (HostError.Length == 0 && PortError.Length == 0 && UserNameError.Length == 0 &&
         PasswordError.Length == 0 && FromAddressError.Length == 0 && RecipientsError.Length == 0));

    public bool UseImplicitTls
    {
        get => useImplicitTls;
        set
        {
            if (SetProperty(ref useImplicitTls, value))
            {
                Port = (value ? EmailConstants.ImplicitTlsPort : EmailConstants.StartTlsPort)
                    .ToString(CultureInfo.InvariantCulture);
                Changed();
            }
        }
    }

    public void SetPasswordState(int length, bool hasNonWhitespaceCharacters)
    {
        newPasswordLength = length;
        hasNonWhitespacePassword = hasNonWhitespaceCharacters;
        OnPropertyChanged(nameof(IsPasswordWatermarkVisible));
        Changed();
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = resources.EmailSettingsLoading;
        try
        {
            EmailSettingsResponse settings = await client.GetAsync(cancellationToken);
            ApplySettings(settings);
            Status = string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Warning, EmailConstants.SettingsLoadFailed);
            Status = resources.EmailSettingsLoadFailed;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> SaveAsync(string password, CancellationToken cancellationToken = default)
    {
        SetPasswordState(password.Length, !string.IsNullOrWhiteSpace(password));
        if (!CanSaveSettings) return false;
        int parsedPort = int.TryParse(Port, NumberStyles.None, CultureInfo.InvariantCulture, out int value) &&
            value == (UseImplicitTls ? EmailConstants.ImplicitTlsPort : EmailConstants.StartTlsPort)
            ? value : UseImplicitTls ? EmailConstants.ImplicitTlsPort : EmailConstants.StartTlsPort;
        IsBusy = true;
        Status = resources.EmailSettingsSaving;
        try
        {
            EmailSettingsResponse saved = await client.SaveAsync(new SaveEmailSettingsRequest(
                IsEnabled, Host.Trim(), parsedPort, UseImplicitTls, UserName.Trim(),
                FromAddress.Trim(), RecipientAddresses.ToArray(), password), cancellationToken);
            ApplySettings(saved);
            Status = resources.EmailSettingsSaved;
            return true;
        }
        catch (IpcException exception) when (exception.ErrorCode == IpcErrorCode.InvalidRequest)
        {
            Status = resources.EmailSettingsInvalid;
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return false; }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Warning, EmailConstants.SettingsSaveFailed);
            Status = resources.EmailSettingsSaveFailed;
            return false;
        }
        finally { IsBusy = false; }
    }

    private void OpenAddRecipient()
    {
        if (!CanAddRecipient) return;
        editingRecipient = null;
        RecipientDraft = string.Empty;
        Changed();
        OnPropertyChanged(nameof(RecipientDialogTitle));
        IsRecipientEditorOpen = true;
    }

    private void OpenEditRecipient(string? address)
    {
        if (IsBusy || address is null || !RecipientAddresses.Contains(address)) return;
        editingRecipient = address;
        RecipientDraft = address;
        Changed();
        OnPropertyChanged(nameof(RecipientDialogTitle));
        IsRecipientEditorOpen = true;
    }

    private void SaveRecipient()
    {
        if (!CanSaveRecipient) return;
        string address = RecipientDraft.Trim();
        if (editingRecipient is null) RecipientAddresses.Add(address);
        else
        {
            int index = RecipientAddresses.IndexOf(editingRecipient);
            if (index < 0) return;
            RecipientAddresses[index] = address;
        }
        IsRecipientEditorOpen = false;
    }

    private void RequestDeleteRecipient(string? address)
    {
        if (IsBusy || address is null || !RecipientAddresses.Contains(address)) return;
        deletingRecipient = address;
        OnPropertyChanged(nameof(DeletingRecipient));
        IsDeleteConfirmationOpen = true;
    }

    private void ConfirmDeleteRecipient()
    {
        if (deletingRecipient is not null) RecipientAddresses.Remove(deletingRecipient);
        IsDeleteConfirmationOpen = false;
    }

    private void ApplySettings(EmailSettingsResponse settings)
    {
        IsEnabled = settings.IsEnabled;
        Host = settings.Host;
        UseImplicitTls = settings.UseImplicitTls;
        Port = settings.Port.ToString(CultureInfo.InvariantCulture);
        UserName = settings.UserName;
        FromAddress = settings.FromAddress;
        RecipientAddresses.Clear();
        foreach (string address in settings.RecipientAddresses) RecipientAddresses.Add(address);
        hasSavedPassword = settings.HasPassword;
        loadedHost = settings.Host;
        loadedPort = settings.Port;
        loadedUseImplicitTls = settings.UseImplicitTls;
        loadedUserName = settings.UserName;
        loadedFromAddress = settings.FromAddress;
        hasLoaded = true;
        PasswordHint = settings.HasPassword ? resources.EmailPasswordSaved : resources.EmailPasswordMissing;
    }

    private bool ConnectionChanged(int parsedPort) =>
        !string.Equals(Host.Trim(), loadedHost, StringComparison.OrdinalIgnoreCase) ||
        parsedPort != loadedPort || UseImplicitTls != loadedUseImplicitTls ||
        !string.Equals(UserName.Trim(), loadedUserName, StringComparison.Ordinal) ||
        !string.Equals(FromAddress.Trim(), loadedFromAddress, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidAddress(string? address) =>
        !string.IsNullOrWhiteSpace(address) && address.Length <= 320 &&
        MailAddress.TryCreate(address, out MailAddress? parsed) &&
        string.Equals(parsed.Address, address, StringComparison.Ordinal);

    private void Changed()
    {
        OnPropertyChanged(nameof(CanSaveSettings));
        OnPropertyChanged(nameof(CanAddRecipient));
        OnPropertyChanged(nameof(CanSaveRecipient));
        OnPropertyChanged(nameof(HostError));
        OnPropertyChanged(nameof(PortError));
        OnPropertyChanged(nameof(UserNameError));
        OnPropertyChanged(nameof(PasswordError));
        OnPropertyChanged(nameof(FromAddressError));
        OnPropertyChanged(nameof(RecipientsError));
        OnPropertyChanged(nameof(RecipientDraftError));
        OnPropertyChanged(nameof(PortExample));
        OnPropertyChanged(nameof(IsHostWatermarkVisible));
        OnPropertyChanged(nameof(IsPortWatermarkVisible));
        OnPropertyChanged(nameof(IsUserNameWatermarkVisible));
        OnPropertyChanged(nameof(IsFromAddressWatermarkVisible));
        OnPropertyChanged(nameof(IsRecipientWatermarkVisible));
    }
}
