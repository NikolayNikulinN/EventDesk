using System.Windows;
using EventDesk.App.ViewModels;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.Views;
using EventDesk.App.Views.Pages;
using EventDesk.App.Services;
using EventDesk.Constants;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Logger.Wrappers;
using EventDesk.Ipc.Abstractions;
using EventDesk.Ipc.Client;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Wrappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace EventDesk.App;

public partial class App : Application
{
    private IHost? host;
    private ILoggerWrapper? logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(e.Args);
        builder.Services.AddSingleton<IResourceWrapper, ResourceWrapper>();
        builder.Services.AddSingleton<ILoggerWrapper, LoggerWrapper>();
        builder.Services.AddSingleton<IIpcClient>(new NamedPipeIpcClient(IpcConstants.PipeName));
        builder.Services.AddSingleton<ICalendarEventClient, CalendarEventIpcClient>();
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
            host.Start();
            MainWindow = host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();

            if (!host.Services.GetRequiredService<INavigationService>().Navigate(typeof(CalendarPage)))
            {
                throw new InvalidOperationException(ApplicationConstants.InitialCalendarNavigationFailed);
            }

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

    protected override void OnExit(ExitEventArgs e)
    {
        if (host is not null)
        {
            try
            {
                logger?.Write(ApplicationConstants.DesktopApplicationStopping);
                host.StopAsync().GetAwaiter().GetResult();
            }
            finally
            {
                host.Dispose();
                logger?.CloseAndFlush();
            }
        }

        base.OnExit(e);
    }
}
