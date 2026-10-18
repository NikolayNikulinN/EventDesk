using System.IO;
using EventDesk.Constants;

namespace EventDesk.App.Notifications;

public sealed class NotificationReceiptStore
{
    private readonly string directory;

    public NotificationReceiptStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        DatabaseConstants.ApplicationDataDirectoryName,
        ReminderProcessingConstants.AppReceiptDirectory))
    {
    }

    public NotificationReceiptStore(string directory)
    {
        this.directory = directory;
        Directory.CreateDirectory(directory);
    }

    public bool TryRecord(Guid deliveryId)
    {
        string path = PathFor(deliveryId);
        try
        {
            using FileStream file = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            file.Flush(flushToDisk: true);
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            return false;
        }
    }

    public void Remove(Guid deliveryId) => File.Delete(PathFor(deliveryId));

    private string PathFor(Guid deliveryId) => Path.Combine(directory,
        $"{deliveryId:N}{ReminderProcessingConstants.ReceiptExtension}");
}
