namespace EventDesk.Constants;

public static class EmailConstants
{
    public const string LegacyRecipientAddressProperty = "RecipientAddress";
    public const string SettingsFileName = "MailSettings.dat";
    public const string SettingsFileExtension = ".tmp";
    public const string NotConfigured = "EmailNotConfigured";
    public const string AuthenticationFailed = "EmailAuthenticationFailed";
    public const string ConnectionFailed = "EmailConnectionFailed";
    public const string Rejected = "EmailRejected";
    public const string SendFailed = "EmailSendFailed";
    public const string InvalidSettings = "Invalid email settings";
    public const string SettingsLoadFailed = "Could not load email settings";
    public const string SettingsSaveFailed = "Could not save email settings";
    public const string PlainTextSubtype = "plain";
    public const string MessageIdDomain = "eventdesk.local";
    public const int StartTlsPort = 587;
    public const int ImplicitTlsPort = 465;
    public const int MaximumRecipients = 50;
}
