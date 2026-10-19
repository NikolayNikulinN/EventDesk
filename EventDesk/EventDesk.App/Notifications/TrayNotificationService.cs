using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using EventDesk.App.Views;
using EventDesk.Contracts.Reminders;
using EventDesk.Resources.Abstract;
using Forms = System.Windows.Forms;

namespace EventDesk.App.Notifications;

public sealed class TrayNotificationService(
    IResourceWrapper resources, NotificationReceiptStore receipts,
    MainWindow mainWindow) : IDisposable
{
    private Forms.NotifyIcon? notifyIcon;
    private Icon? trayIcon;
    private Image? openMenuIcon;
    private Image? exitMenuIcon;

    public void Initialize()
    {
        using var stream = System.Windows.Application.GetResourceStream(
            new Uri(resources.AppTrayIconPath, UriKind.Relative))?.Stream
            ?? throw new InvalidOperationException(resources.AppTrayIconPath);
        using Icon source = new(stream, Forms.SystemInformation.SmallIconSize);
        trayIcon = (Icon)source.Clone();
        openMenuIcon = LoadMenuIcon(resources.TrayOpenIconPath);
        exitMenuIcon = LoadMenuIcon(resources.TrayExitIconPath);

        Forms.ContextMenuStrip menu = new();
        menu.Items.Add(resources.OpenApplication, openMenuIcon, (_, _) => OpenWindow());
        menu.Items.Add(resources.ExitApplication, exitMenuIcon, (_, _) =>
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _ = ((App)System.Windows.Application.Current).ExitFromTrayAsync();
            })));

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = trayIcon,
            Text = resources.TrayTooltip,
            ContextMenuStrip = menu,
            Visible = true
        };
        notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left) OpenWindow();
        };
        notifyIcon.BalloonTipClicked += (_, _) => OpenWindow();
    }

    public async Task ShowAsync(DesktopReminderMessage message, CancellationToken cancellationToken)
    {
        if (!receipts.TryRecord(message.DeliveryId)) return;
        bool shown = false;
        try
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (notifyIcon is null) throw new InvalidOperationException();
                notifyIcon.ShowBalloonTip(10000, resources.ReminderNotificationTitle,
                    string.IsNullOrWhiteSpace(message.EventDescription)
                        ? message.EventTitle : $"{message.EventTitle}{Environment.NewLine}{message.EventDescription}",
                    Forms.ToolTipIcon.Info);
                shown = true;
            }, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken);
        }
        catch
        {
            if (!shown) receipts.Remove(message.DeliveryId);
            throw;
        }
    }

    public void Dispose()
    {
        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
            notifyIcon.ContextMenuStrip?.Dispose();
            notifyIcon.Dispose();
        }
        trayIcon?.Dispose();
        openMenuIcon?.Dispose();
        exitMenuIcon?.Dispose();
    }

    private static Image LoadMenuIcon(string path)
    {
        using var stream = System.Windows.Application.GetResourceStream(
            new Uri(path, UriKind.Relative))?.Stream
            ?? throw new InvalidOperationException(path);
        BitmapDecoder decoder = BitmapDecoder.Create(stream,
            BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(decoder.Frames[0]);
        using MemoryStream png = new();
        encoder.Save(png);
        png.Position = 0;
        using Image source = Image.FromStream(png);
        return new Bitmap(source, Forms.SystemInformation.SmallIconSize);
    }

    public void OpenWindow() => System.Windows.Application.Current.Dispatcher.Invoke(() =>
    {
        if (!mainWindow.IsVisible) mainWindow.Show();
        if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
            mainWindow.WindowState = System.Windows.WindowState.Normal;
        mainWindow.Activate();
    });
}
