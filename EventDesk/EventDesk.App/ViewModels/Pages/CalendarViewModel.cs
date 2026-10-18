using System.Collections.ObjectModel;
using System.Globalization;
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
using EventDesk.Resources.Models;
using EventDesk.Resources.Theme;

namespace EventDesk.App.ViewModels.Pages;

public sealed class CalendarViewModel : ObservableObject
{
    private readonly ICalendarEventClient eventClient;
    private readonly ILoggerWrapper logger;
    private DateTime? selectedDate = DateTime.Today;
    private string weekRange = string.Empty;
    private string status = string.Empty;
    private string errorMessage = string.Empty;
    private string editorError = string.Empty;
    private string editorHeading = string.Empty;
    private string eventTitle = string.Empty;
    private string eventDescription = string.Empty;
    private DateTime? startDate;
    private DateTime? endDate;
    private string startTime = string.Empty;
    private string endTime = string.Empty;
    private double startMinuteOfDay = 540;
    private double endMinuteOfDay = 600;
    private bool isEnabled = true;
    private bool isEditing;
    private bool isLoading;
    private bool isSaving;
    private bool isDeleteConfirmationVisible;
    private ReminderOption? selectedReminder;
    private CalendarColorOption? selectedColor;
    private CalendarEventResponse? editedEvent;
    private bool reminderChanged;
    private int loadVersion;

    public CalendarViewModel(ICalendarEventClient eventClient, IResourceWrapper resources, ILoggerWrapper logger)
    {
        this.eventClient = eventClient;
        this.logger = logger;
        Resources = resources;
        ColorOptions = CalendarColorPalette.CreateOptions(resources);
        selectedColor = ColorOptions[0];
        ReminderOptions =
        [
            new ReminderOption(0, resources.NoReminder),
            new ReminderOption(5, resources.ReminderFiveMinutes),
            new ReminderOption(15, resources.ReminderFifteenMinutes),
            new ReminderOption(30, resources.ReminderThirtyMinutes),
            new ReminderOption(60, resources.ReminderOneHour)
        ];
        selectedReminder = ReminderOptions[0];
        HourLabels = Enumerable.Range(0, 24)
            .Select(hour => DateTime.Today.AddHours(hour).ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture))
            .ToArray();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        PreviousWeekCommand = new RelayCommand(() => MoveWeek(-7));
        NextWeekCommand = new RelayCommand(() => MoveWeek(7));
        TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
        SelectDateCommand = new RelayCommand<DateTime>(date => SelectedDate = date);
        NewEventCommand = new RelayCommand(OpenNewEvent);
        EditEventCommand = new RelayCommand<CalendarEventItem>(OpenEvent);
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        SaveEventCommand = new AsyncRelayCommand(SaveAsync, CanSaveEvent);
        RequestDeleteCommand = new RelayCommand(() => IsDeleteConfirmationVisible = true);
        CancelDeleteCommand = new RelayCommand(() => IsDeleteConfirmationVisible = false);
        DeleteEventCommand = new AsyncRelayCommand(DeleteAsync);
        UpdateWeekRange();
    }

    public IResourceWrapper Resources { get; }

    public ObservableCollection<CalendarDay> Days { get; } = [];

    public IReadOnlyList<string> HourLabels { get; }

    public IReadOnlyList<ReminderOption> ReminderOptions { get; }

    public IReadOnlyList<CalendarColorOption> ColorOptions { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IRelayCommand PreviousWeekCommand { get; }

    public IRelayCommand NextWeekCommand { get; }

    public IRelayCommand TodayCommand { get; }

    public IRelayCommand<DateTime> SelectDateCommand { get; }

    public IRelayCommand NewEventCommand { get; }

    public IRelayCommand<CalendarEventItem> EditEventCommand { get; }

    public IRelayCommand CancelEditCommand { get; }

    public IAsyncRelayCommand SaveEventCommand { get; }

    public IRelayCommand RequestDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public IAsyncRelayCommand DeleteEventCommand { get; }

    public DateTime? SelectedDate
    {
        get => selectedDate;
        set
        {
            if (value is not { } date || !SetProperty(ref selectedDate, date.Date))
            {
                return;
            }

            UpdateWeekRange();
            _ = RefreshCommand.ExecuteAsync(null);
        }
    }

    public string WeekRange
    {
        get => weekRange;
        private set => SetProperty(ref weekRange, value);
    }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool IsSaving
    {
        get => isSaving;
        private set
        {
            if (SetProperty(ref isSaving, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsEditing
    {
        get => isEditing;
        private set
        {
            if (SetProperty(ref isEditing, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanDeleteEvent => editedEvent is not null;

    public bool IsDeleteConfirmationVisible
    {
        get => isDeleteConfirmationVisible;
        private set => SetProperty(ref isDeleteConfirmationVisible, value);
    }

    public string EditorHeading
    {
        get => editorHeading;
        private set => SetProperty(ref editorHeading, value);
    }

    public string EditorError
    {
        get => editorError;
        private set => SetProperty(ref editorError, value);
    }

    public string EventTitle
    {
        get => eventTitle;
        set
        {
            if (SetProperty(ref eventTitle, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string EventDescription
    {
        get => eventDescription;
        set
        {
            if (SetProperty(ref eventDescription, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public DateTime? StartDate
    {
        get => startDate;
        set
        {
            if (SetProperty(ref startDate, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public DateTime? EndDate
    {
        get => endDate;
        set
        {
            if (SetProperty(ref endDate, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StartTime
    {
        get => startTime;
        set
        {
            if (!SetProperty(ref startTime, value))
            {
                return;
            }

            if (TimeOnly.TryParse(value, CultureInfo.CurrentCulture, out TimeOnly parsed))
            {
                double minute = parsed.Hour * 60 + parsed.Minute;
                if (startMinuteOfDay != minute)
                {
                    startMinuteOfDay = minute;
                    OnPropertyChanged(nameof(StartMinuteOfDay));
                }
            }

            SaveEventCommand.NotifyCanExecuteChanged();
        }
    }

    public string EndTime
    {
        get => endTime;
        set
        {
            if (!SetProperty(ref endTime, value))
            {
                return;
            }

            if (TimeOnly.TryParse(value, CultureInfo.CurrentCulture, out TimeOnly parsed))
            {
                double minute = parsed.Hour * 60 + parsed.Minute;
                if (endMinuteOfDay != minute)
                {
                    endMinuteOfDay = minute;
                    OnPropertyChanged(nameof(EndMinuteOfDay));
                }
            }

            SaveEventCommand.NotifyCanExecuteChanged();
        }
    }

    public double StartMinuteOfDay
    {
        get => startMinuteOfDay;
        set
        {
            if (SetProperty(ref startMinuteOfDay, Math.Clamp(Math.Round(value), 0, 1439)))
            {
                StartTime = DateTime.Today.AddMinutes(startMinuteOfDay)
                    .ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
            }
        }
    }

    public double EndMinuteOfDay
    {
        get => endMinuteOfDay;
        set
        {
            if (SetProperty(ref endMinuteOfDay, Math.Clamp(Math.Round(value), 0, 1439)))
            {
                EndTime = DateTime.Today.AddMinutes(endMinuteOfDay)
                    .ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
            }
        }
    }

    public CalendarColorOption? SelectedColor
    {
        get => selectedColor;
        set
        {
            if (SetProperty(ref selectedColor, value))
            {
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }

    public ReminderOption? SelectedReminder
    {
        get => selectedReminder;
        set
        {
            if (SetProperty(ref selectedReminder, value))
            {
                reminderChanged = true;
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private void MoveWeek(int days) => SelectedDate = (SelectedDate ?? DateTime.Today).AddDays(days);

    private void UpdateWeekRange()
    {
        DateTime monday = GetMonday(SelectedDate ?? DateTime.Today);
        WeekRange = string.Format(CultureInfo.CurrentCulture, Resources.WeekRangeFormat, monday, monday.AddDays(6));
    }

    private static DateTime GetMonday(DateTime date) =>
        date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        int version = ++loadVersion;
        DateTime monday = GetMonday(SelectedDate ?? DateTime.Today);
        DateTime nextMonday = monday.AddDays(7);
        IsLoading = true;
        Status = Resources.LoadingEvents;
        ErrorMessage = string.Empty;
        Days.Clear();
        for (int index = 0; index < 7; index++)
        {
            Days.Add(new CalendarDay(monday.AddDays(index)));
        }
        try
        {
            IReadOnlyList<CalendarEventResponse> events = await eventClient.GetByPeriodAsync(
                ToLocalOffset(monday), ToLocalOffset(nextMonday), cancellationToken);
            if (version != loadVersion)
            {
                return;
            }

            Days.Clear();
            for (int index = 0; index < 7; index++)
            {
                CalendarDay day = new(monday.AddDays(index));
                foreach (CalendarEventResponse calendarEvent in events)
                {
                    if (calendarEvent.StartsAtUtc.ToLocalTime().Date < day.Date.AddDays(1) &&
                        calendarEvent.EndsAtUtc.ToLocalTime().DateTime > day.Date)
                    {
                        day.Events.Add(new CalendarEventItem(calendarEvent, day.Date, Resources.EventTimeRangeFormat));
                    }
                }

                day.ArrangeEvents();

                Days.Add(day);
            }

            Status = events.Count == 0 ? Resources.NoEventsThisWeek : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (version == loadVersion)
            {
                Status = string.Empty;
            }
        }
        catch (Exception exception)
        {
            if (version == loadVersion)
            {
                logger.Write(exception, LogLevel.Warning, IpcConstants.PipeRequestFailed);
                Status = string.Empty;
                ErrorMessage = GetErrorMessage(exception, Resources.CalendarLoadFailed);
            }
        }
        finally
        {
            if (version == loadVersion)
            {
                IsLoading = false;
            }
        }
    }

    private void OpenNewEvent()
    {
        editedEvent = null;
        OnPropertyChanged(nameof(CanDeleteEvent));
        EventTitle = string.Empty;
        EventDescription = string.Empty;
        StartDate = SelectedDate ?? DateTime.Today;
        EndDate = StartDate;
        StartTime = DateTime.Today.AddHours(9).ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
        EndTime = DateTime.Today.AddHours(10).ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
        IsEnabled = true;
        SelectedReminder = ReminderOptions[0];
        SelectedColor = ColorOptions[0];
        reminderChanged = false;
        EditorHeading = Resources.NewEvent;
        EditorError = string.Empty;
        IsDeleteConfirmationVisible = false;
        IsEditing = true;
    }

    private void OpenEvent(CalendarEventItem? item)
    {
        if (item is null)
        {
            return;
        }

        editedEvent = item.Source;
        OnPropertyChanged(nameof(CanDeleteEvent));
        EventTitle = item.Source.Title;
        EventDescription = item.Source.Description ?? string.Empty;
        DateTimeOffset start = item.Source.StartsAtUtc.ToLocalTime();
        DateTimeOffset end = item.Source.EndsAtUtc.ToLocalTime();
        StartDate = start.Date;
        EndDate = end.Date;
        StartTime = start.ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
        EndTime = end.ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
        IsEnabled = item.Source.IsEnabled;
        ReminderRuleDto? simpleReminder = item.Source.Reminders.FirstOrDefault(IsSimpleDesktopReminder);
        SelectedReminder = ReminderOptions.FirstOrDefault(option =>
            option.Minutes == (int)(simpleReminder?.LeadTime.TotalMinutes ?? 0)) ?? ReminderOptions[0];
        SelectedColor = ColorOptions.FirstOrDefault(option =>
            string.Equals(option.ColorHex, item.Source.ColorHex, StringComparison.OrdinalIgnoreCase)) ?? ColorOptions[0];
        reminderChanged = false;
        EditorHeading = Resources.EditEvent;
        EditorError = string.Empty;
        IsDeleteConfirmationVisible = false;
        IsEditing = true;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!IsEditing || IsSaving || !TryBuildEvent(out SaveEventDto? model))
        {
            return;
        }

        IsSaving = true;
        EditorError = string.Empty;
        try
        {
            CalendarEventResponse saved = editedEvent is { } current
                ? await eventClient.UpdateAsync(current.Id, model!, cancellationToken)
                : await eventClient.CreateAsync(model!, cancellationToken);
            IsEditing = false;
            selectedDate = saved.StartsAtUtc.ToLocalTime().Date;
            OnPropertyChanged(nameof(SelectedDate));
            UpdateWeekRange();
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Warning, IpcConstants.PipeRequestFailed);
            EditorError = GetErrorMessage(exception, Resources.EventSaveFailed);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSaveEvent() => IsEditing && !IsSaving &&
        !string.IsNullOrWhiteSpace(EventTitle) && EventTitle.Length <= 500 &&
        !string.IsNullOrWhiteSpace(EventDescription) && EventDescription.Length <= 2000 &&
        StartDate is not null && EndDate is not null &&
        TimeOnly.TryParse(StartTime, CultureInfo.CurrentCulture, out _) &&
        TimeOnly.TryParse(EndTime, CultureInfo.CurrentCulture, out _) &&
        SelectedReminder is not null && SelectedColor is not null;

    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (editedEvent is not { } current || !IsDeleteConfirmationVisible || IsSaving)
        {
            return;
        }

        IsSaving = true;
        EditorError = string.Empty;
        try
        {
            await eventClient.DeleteAsync(current.Id, cancellationToken);
            IsEditing = false;
            IsDeleteConfirmationVisible = false;
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Warning, IpcConstants.PipeRequestFailed);
            EditorError = GetErrorMessage(exception, Resources.EventDeleteFailed);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool TryBuildEvent(out SaveEventDto? model)
    {
        model = null;
        if (string.IsNullOrWhiteSpace(EventTitle))
        {
            EditorError = Resources.EventTitleRequired;
            return false;
        }

        if (string.IsNullOrWhiteSpace(EventDescription))
        {
            EditorError = Resources.EventDescriptionRequired;
            return false;
        }

        if (EventTitle.Length > 500 || EventDescription.Length > 2000 ||
            SelectedReminder is null || SelectedColor is null)
        {
            EditorError = Resources.InvalidEventData;
            return false;
        }

        if (StartDate is not { } startDateValue || EndDate is not { } endDateValue ||
            !TimeOnly.TryParse(StartTime, CultureInfo.CurrentCulture, out TimeOnly startTimeValue) ||
            !TimeOnly.TryParse(EndTime, CultureInfo.CurrentCulture, out TimeOnly endTimeValue))
        {
            EditorError = Resources.EventDateTimeRequired;
            return false;
        }

        DateTime localStart = startDateValue.Date.Add(startTimeValue.ToTimeSpan());
        DateTime localEnd = endDateValue.Date.Add(endTimeValue.ToTimeSpan());
        if (TimeZoneInfo.Local.IsInvalidTime(localStart) || TimeZoneInfo.Local.IsInvalidTime(localEnd))
        {
            EditorError = Resources.EventTimeInvalid;
            return false;
        }

        if (localEnd <= localStart)
        {
            EditorError = Resources.EventEndAfterStart;
            return false;
        }

        List<ReminderRuleDto> reminders = editedEvent?.Reminders
            .Where(rule => !reminderChanged || !IsSimpleDesktopReminder(rule)).ToList() ?? [];
        if ((editedEvent is null || reminderChanged) && SelectedReminder is { Minutes: > 0 } reminder)
        {
            reminders.Add(new ReminderRuleDto(TimeSpan.FromMinutes(reminder.Minutes), ReminderChannelCode.Desktop, 1, null));
        }

        model = new SaveEventDto(EventTitle.Trim(), ToLocalOffset(localStart), ToLocalOffset(localEnd),
            TimeZoneInfo.Local.Id, IsEnabled, reminders, EventDescription.Trim(), SelectedColor.ColorHex);
        EditorError = string.Empty;
        return true;
    }

    private static bool IsSimpleDesktopReminder(ReminderRuleDto rule) =>
        rule.Channel == ReminderChannelCode.Desktop && rule.OccurrenceCount == 1 && rule.RepeatInterval is null;

    private static DateTimeOffset ToLocalOffset(DateTime local) =>
        new(local, TimeZoneInfo.Local.GetUtcOffset(local));

    private string GetErrorMessage(Exception exception, string fallback) => exception is IpcException ipcException
        ? ipcException.ErrorCode switch
        {
            IpcErrorCode.TimedOut => Resources.ServiceRequestTimedOut,
            IpcErrorCode.TransportError or IpcErrorCode.AccessDenied => Resources.ServiceUnavailable,
            IpcErrorCode.InvalidRequest => Resources.InvalidEventData,
            IpcErrorCode.NotFound => Resources.EventNoLongerExists,
            _ => fallback
        }
        : fallback;
}
