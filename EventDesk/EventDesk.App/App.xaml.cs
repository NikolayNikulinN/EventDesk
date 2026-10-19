using System.Windows;
using System.Security.Principal;
using EventDesk.App.ViewModels;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.Views;
using EventDesk.App.Views.Pages;
using EventDesk.App.Services;
using EventDesk.App.Notifications;
using EventDesk.Constants;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Logger.Wrappers;
using EventDesk.Ipc.Abstractions;
using EventDesk.Ipc.Client;
using EventDesk.Ipc.Server;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Behaviors;
using EventDesk.Resources.Wrappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace EventDesk.App;

public partial class App : System.Windows.Application
{
    private IHost? host;
    private ILoggerWrapper? logger;
    private bool isExiting;
    private bool hostStopped;
    private Mutex? instanceMutex;
    private EventWaitHandle? activationEvent;
    private RegisteredWaitHandle? activationRegistration;
    private bool ownsInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        bool startInBackground = e.Args.Contains(ApplicationConstants.BackgroundArgument,
            StringComparer.OrdinalIgnoreCase);
        string userSid = WindowsIdentity.GetCurrent().User?.Value
            ?? throw new InvalidOperationException(ApplicationConstants.CurrentUserSidUnavailable);
        instanceMutex = new Mutex(true, ApplicationConstants.InstanceMutexPrefix + userSid,
            out bool createdNew);
        ownsInstanceMutex = createdNew;
        if (!createdNew)
        {
            if (!startInBackground)
                ActivateRunningInstance(userSid);
            Shutdown();
            return;
        }

        activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset,
            ApplicationConstants.ActivationEventPrefix + userSid);
        activationRegistration = ThreadPool.RegisterWaitForSingleObject(activationEvent, (_, _) =>
        {
            if (!Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(() =>
                    host?.Services.GetRequiredService<TrayNotificationService>().OpenWindow()));
        }, null, Timeout.Infinite, false);

        InteractiveCursorBehavior.Register();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(e.Args.Where(argument =>
            !string.Equals(argument, ApplicationConstants.BackgroundArgument,
                StringComparison.OrdinalIgnoreCase)).ToArray());
        builder.Services.AddSingleton<IResourceWrapper, ResourceWrapper>();
        builder.Services.AddSingleton<ILoggerWrapper, LoggerWrapper>();
        builder.Services.AddSingleton<IIpcClient>(new NamedPipeIpcClient(IpcConstants.PipeName));
        builder.Services.AddSingleton<ICalendarEventClient, CalendarEventIpcClient>();
        builder.Services.AddSingleton<IReminderHistoryClient, ReminderHistoryIpcClient>();
        builder.Services.AddSingleton<IEmailSettingsClient, EmailSettingsIpcClient>();
        builder.Services.AddSingleton<IDesktopReminderServer, NamedPipeDesktopReminderServer>();
        builder.Services.AddSingleton<NotificationReceiptStore>();
        builder.Services.AddSingleton<TrayNotificationService>();
        builder.Services.AddHostedService<DesktopReminderReceiverWorker>();
        builder.Services.AddNavigationViewPageProvider();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddTransient<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<AboutPage>();

        host = builder.Build();
        logger = host.Services.GetRequiredService<ILoggerWrapper>();
        try
        {
            MainWindow = host.Services.GetRequiredService<MainWindow>();
            if (startInBackground)
            {
                MainWindow.Opacity = 0;
                MainWindow.ShowInTaskbar = false;
            }
            MainWindow.Show();

            if (!host.Services.GetRequiredService<INavigationService>().Navigate(typeof(CalendarPage)))
            {
                throw new InvalidOperationException(ApplicationConstants.InitialCalendarNavigationFailed);
            }

            if (startInBackground)
            {
                MainWindow.Hide();
                MainWindow.Opacity = 1;
                MainWindow.ShowInTaskbar = true;
            }

            host.Services.GetRequiredService<TrayNotificationService>().Initialize();
            host.Start();

            logger.Write(ApplicationConstants.DesktopApplicationStarted);
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Fatal, ApplicationConstants.DesktopApplicationFailedToStart);
            logger.CloseAndFlush();
            host.Dispose();
            host = null;
            logger = null;

            throw;
        }
    }

    public async Task ExitFromTrayAsync()
    {
        if (isExiting) return;
        isExiting = true;

        if (MainWindow is MainWindow mainWindow)
            mainWindow.PrepareForExit();

        try
        {
            if (host is not null)
            {
                logger?.Write(ApplicationConstants.DesktopApplicationStopping);
                await host.StopAsync();
            }
        }
        catch (Exception exception)
        {
            logger?.Write(exception, LogLevel.Error, ApplicationConstants.DesktopApplicationStopFailed);
        }
        finally
        {
            hostStopped = true;
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (host is not null)
            {
                try
                {
                    if (!hostStopped)
                    {
                        logger?.Write(ApplicationConstants.DesktopApplicationStopping);
                        host.StopAsync().GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    host.Dispose();
                    logger?.CloseAndFlush();
                }
            }
        }
        finally
        {
            activationRegistration?.Unregister(null);
            activationEvent?.Dispose();
            if (ownsInstanceMutex) instanceMutex?.ReleaseMutex();
            instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }

    private static void ActivateRunningInstance(string userSid)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (EventWaitHandle.TryOpenExisting(ApplicationConstants.ActivationEventPrefix + userSid,
                out EventWaitHandle? existingEvent))
            {
                using (existingEvent) existingEvent.Set();
                return;
            }

            Thread.Sleep(50);
        }
    }
}
