namespace EventDesk.Constants;

public static class ReminderProcessingConstants
{
    public const string DeliveryFailed = "Reminder delivery {DeliveryId} failed on attempt {AttemptNumber}";
    public const string ProcessingFailed = "Reminder processing cycle failed";
    public const string NotificationServerFailed = "Reminder notification receiver stopped unexpectedly";
    public const string InvalidAcknowledgement = "Reminder acknowledgement does not match the delivery";
    public const string EmailUnavailable = "Email delivery is not configured";
    public const string ReceiverUnavailable = "The desktop notification receiver is unavailable";
    public const string InvalidNotification = "Invalid reminder notification";
    public const string DeliveryTimedOut = "DeliveryTimedOut";
    public const string EndpointUnavailable = "EndpointUnavailable";
    public const string UnsupportedChannel = "UnsupportedChannel";
    public const string DeliveryError = "DeliveryError";
    public const string Interrupted = "Interrupted";
    public const string AppReceiptDirectory = "NotificationReceipts";
    public const string ReceiptExtension = ".receipt";
    public const string HistoryLoadFailed = "Could not load reminder history";
    public const int HistoryPageSize = 200;
}
