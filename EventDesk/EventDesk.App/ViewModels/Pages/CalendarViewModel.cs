using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventDesk.App.Models;
using EventDesk.App.Services;
using EventDesk.Constants;
using EventDesk.Contracts.Events;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class CalendarViewModel : ObservableObject
{
    private readonly ICalendarEventClient eventClient;
    private readonly IResourceWrapper resources;
    private readonly ILoggerWrapper logger;
    private string status = string.Empty;

    public CalendarViewModel(
        ICalendarEventClient eventClient,
        IResourceWrapper resources,
        ILoggerWrapper logger)
    {
        this.eventClient = eventClient;
        this.resources = resources;
        this.logger = logger;
        Title = resources.Calendar;
        RefreshLabel = resources.RefreshEvents;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public string Title { get; }

    public string RefreshLabel { get; }

    public ObservableCollection<CalendarEventItem> Events { get; } = [];

    public IAsyncRelayCommand RefreshCommand { get; }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Status = resources.LoadingEvents;
        Events.Clear();
        try
        {
            (DateTimeOffset from, DateTimeOffset to) = GetCurrentWeek();
            IReadOnlyList<CalendarEventResponse> result =
                await eventClient.GetByPeriodAsync(from, to, cancellationToken);
            foreach (CalendarEventResponse calendarEvent in result)
            {
                Events.Add(new CalendarEventItem(
                    calendarEvent.Id,
                    calendarEvent.Title,
                    calendarEvent.StartsAtUtc.ToLocalTime()));
            }

            Status = Events.Count == 0 ? resources.NoEventsThisWeek : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Status = string.Empty;
        }
        catch (IpcException exception)
        {
            logger.Write(exception, LogLevel.Warning, IpcConstants.PipeRequestFailed);
            Status = exception.ErrorCode switch
            {
                IpcErrorCode.TimedOut => resources.ServiceRequestTimedOut,
                IpcErrorCode.TransportError or IpcErrorCode.AccessDenied => resources.ServiceUnavailable,
                _ => resources.CalendarLoadFailed
            };
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Error, IpcConstants.PipeRequestFailed);
            Status = resources.CalendarLoadFailed;
        }
    }

    private static (DateTimeOffset From, DateTimeOffset To) GetCurrentWeek()
    {
        DateTime today = DateTime.Today;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        DateTime monday = today.AddDays(-daysSinceMonday);
        DateTime nextMonday = monday.AddDays(7);
        return (
            new DateTimeOffset(monday, TimeZoneInfo.Local.GetUtcOffset(monday)),
            new DateTimeOffset(nextMonday, TimeZoneInfo.Local.GetUtcOffset(nextMonday)));
    }
}
