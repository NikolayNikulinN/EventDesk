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
    private readonly EventOverlapDetector overlapDetector;
    private readonly ILoggerWrapper logger;
    private DateTime? selectedDate = DateTime.Today;
    private string weekRange = string.Empty;
    private string status = string.Empty;
    private string errorMessage = string.Empty;
    private string editorError = string.Empty;
    private string overlapWarning = string.Empty;
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
    private ReminderOption? selectedEmailReminder;
    private EventRecurrenceOption? selectedRecurrence;
    private NotificationIntervalOption? selectedNotificationInterval;
    private int selectedMonthOfYear;
    private int selectedDayOfMonth;
    private CalendarColorOption? selectedColor;
    private CalendarEventResponse? editedEvent;
    private bool reminderChanged;
    private bool emailReminderChanged;
    private OverlapSchedule? pendingOverlapSchedule;
    private int loadVersion;

    public CalendarViewModel(ICalendarEventClient eventClient, IResourceWrapper resources, ILoggerWrapper logger)
    {
        this.eventClient = eventClient;
        overlapDetector = new EventOverlapDetector(eventClient);
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
        selectedEmailReminder = ReminderOptions[0];
        RecurrenceOptions =
        [
            new EventRecurrenceOption(EventRecurrenceCode.None, resources.EventRepeatOnce),
            new EventRecurrenceOption(EventRecurrenceCode.Yearly, resources.EventRepeatYearly),
            new EventRecurrenceOption(EventRecurrenceCode.Monthly, resources.EventRepeatMonthly)
        ];
        selectedRecurrence = RecurrenceOptions[0];
        NotificationIntervalOptions =
        [
            new NotificationIntervalOption(30, resources.EveryThirtyMinutes),
            new NotificationIntervalOption(60, resources.EveryHour),
            new NotificationIntervalOption(120, resources.EveryTwoHours),
            new NotificationIntervalOption(180, resources.EveryThreeHours),
            new NotificationIntervalOption(240, resources.EveryFourHours),
            new NotificationIntervalOption(360, resources.EverySixHours)
        ];
        selectedNotificationInterval = NotificationIntervalOptions[3];
        selectedMonthOfYear = DateTime.Today.Month;
        selectedDayOfMonth = DateTime.Today.Day;
        MonthsOfYear = Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)))
            .ToArray();
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

    public IReadOnlyList<EventRecurrenceOption> RecurrenceOptions { get; }

    public IReadOnlyList<NotificationIntervalOption> NotificationIntervalOptions { get; }

    public IReadOnlyList<int> DaysOfMonth { get; } = Enumerable.Range(1, 31).ToArray();

    public IReadOnlyList<MonthOption> MonthsOfYear { get; }

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

    public string OverlapWarning
    {
        get => overlapWarning;
        private set => SetProperty(ref overlapWarning, value);
    }

    public string SaveButtonLabel => pendingOverlapSchedule is null ? Resources.SaveEvent : Resources.SaveAnyway;

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
                ClearOverlapWarning();
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
                ClearOverlapWarning();
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

            ClearOverlapWarning();
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
            OnPropertyChanged(nameof(NotificationTimesPreview));
            OnPropertyChanged(nameof(NotificationWindowError));
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

            ClearOverlapWarning();
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
            OnPropertyChanged(nameof(NotificationTimesPreview));
            OnPropertyChanged(nameof(NotificationWindowError));
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
        set
        {
            if (SetProperty(ref isEnabled, value))
            {
                ClearOverlapWarning();
            }
        }
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

    public ReminderOption? SelectedEmailReminder
    {
        get => selectedEmailReminder;
        set
        {
            if (SetProperty(ref selectedEmailReminder, value))
            {
                emailReminderChanged = true;
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public EventRecurrenceOption? SelectedRecurrence
    {
        get => selectedRecurrence;
        set
        {
            EventRecurrenceCode oldCode = selectedRecurrence?.Code ?? EventRecurrenceCode.None;
            if (!SetProperty(ref selectedRecurrence, value))
            {
                return;
            }

            ClearOverlapWarning();
            if (oldCode == EventRecurrenceCode.None && IsRecurringEvent && editedEvent is null)
            {
                StartTime = DateTime.Today.AddHours(11).ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
                EndTime = DateTime.Today.AddHours(17).ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture);
            }

            OnPropertyChanged(nameof(IsOneTimeEvent));
            OnPropertyChanged(nameof(IsYearlyEvent));
            OnPropertyChanged(nameof(IsMonthlyEvent));
            OnPropertyChanged(nameof(IsRecurringEvent));
            OnPropertyChanged(nameof(StartTimeLabel));
            OnPropertyChanged(nameof(EndTimeLabel));
            OnPropertyChanged(nameof(NotificationTimesPreview));
            OnPropertyChanged(nameof(NotificationWindowError));
            SaveEventCommand.NotifyCanExecuteChanged();
        }
    }

    public NotificationIntervalOption? SelectedNotificationInterval
    {
        get => selectedNotificationInterval;
        set
        {
            if (SetProperty(ref selectedNotificationInterval, value))
            {
                ClearOverlapWarning();
                OnPropertyChanged(nameof(NotificationTimesPreview));
                OnPropertyChanged(nameof(NotificationWindowError));
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int SelectedDayOfMonth
    {
        get => selectedDayOfMonth;
        set
        {
            if (SetProperty(ref selectedDayOfMonth, value))
            {
                ClearOverlapWarning();
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int SelectedMonthOfYear
    {
        get => selectedMonthOfYear;
        set
        {
            if (SetProperty(ref selectedMonthOfYear, value))
            {
                ClearOverlapWarning();
                SaveEventCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsOneTimeEvent => SelectedRecurrence?.Code == EventRecurrenceCode.None;

    public bool IsYearlyEvent => SelectedRecurrence?.Code == EventRecurrenceCode.Yearly;

    public bool IsMonthlyEvent => SelectedRecurrence?.Code == EventRecurrenceCode.Monthly;

    public bool IsRecurringEvent => IsYearlyEvent || IsMonthlyEvent;

    public string StartTimeLabel => IsRecurringEvent ? Resources.FirstNotification : Resources.StartTime;

    public string EndTimeLabel => IsRecurringEvent ? Resources.LastNotification : Resources.EndTime;

    public string NotificationTimesPreview
    {
        get
        {
            if (!IsRecurringEvent || !TryGetRecurringWindow(out TimeOnly first, out TimeOnly last, out int interval))
            {
                return string.Empty;
            }

            return string.Join(", ", Enumerable.Range(0, (last.Hour * 60 + last.Minute - first.Hour * 60 - first.Minute) / interval + 1)
                .Select(index => first.AddMinutes(index * interval)
                    .ToString(Resources.Time24HourFormat, CultureInfo.CurrentCulture)));
        }
    }

    public string NotificationWindowError => IsRecurringEvent &&
        !TryGetRecurringWindow(out _, out _, out _) ? Resources.NotificationWindowInvalid : string.Empty;

    private bool TryGetRecurringWindow(out TimeOnly first, out TimeOnly last, out int interval)
    {
        interval = SelectedNotificationInterval?.Minutes ?? 0;
        bool hasFirstTime = TimeOnly.TryParse(StartTime, CultureInfo.CurrentCulture, out first);
        bool hasLastTime = TimeOnly.TryParse(EndTime, CultureInfo.CurrentCulture, out last);
        return hasFirstTime && hasLastTime &&
            interval > 0 && first < last &&
            (last.Hour * 60 + last.Minute - first.Hour * 60 - first.Minute) % interval == 0;
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
        SelectedEmailReminder = ReminderOptions[0];
        SelectedRecurrence = RecurrenceOptions[0];
        SelectedNotificationInterval = NotificationIntervalOptions[3];
        SelectedMonthOfYear = StartDate.Value.Month;
        SelectedDayOfMonth = StartDate.Value.Day;
        SelectedColor = ColorOptions[0];
        reminderChanged = false;
        emailReminderChanged = false;
        EditorHeading = Resources.NewEvent;
        EditorError = string.Empty;
        ClearOverlapWarning();
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
        ReminderRuleDto? simpleEmailReminder = item.Source.Reminders.FirstOrDefault(IsSimpleEmailReminder);
        SelectedEmailReminder = ReminderOptions.FirstOrDefault(option =>
            option.Minutes == (int)(simpleEmailReminder?.LeadTime.TotalMinutes ?? 0)) ?? ReminderOptions[0];
        SelectedRecurrence = RecurrenceOptions.First(option => option.Code == item.Source.Recurrence);
        SelectedNotificationInterval = NotificationIntervalOptions.FirstOrDefault(option =>
            option.Minutes == item.Source.NotificationIntervalMinutes) ?? NotificationIntervalOptions[3];
        SelectedMonthOfYear = item.Source.RecurrenceMonth ?? start.Month;
        SelectedDayOfMonth = item.Source.RecurrenceDay ?? start.Day;
        SelectedColor = ColorOptions.FirstOrDefault(option =>
            string.Equals(option.ColorHex, item.Source.ColorHex, StringComparison.OrdinalIgnoreCase)) ?? ColorOptions[0];
        reminderChanged = false;
        emailReminderChanged = false;
        EditorHeading = Resources.EditEvent;
        EditorError = string.Empty;
        ClearOverlapWarning();
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
            OverlapSchedule schedule = OverlapSchedule.From(model!);
            if (pendingOverlapSchedule != schedule)
            {
                CalendarEventResponse? conflict = await overlapDetector.FindFirstAsync(
                    model!, editedEvent?.Id, SelectedDate ?? DateTime.Today, cancellationToken);
                if (conflict is not null)
                {
                    pendingOverlapSchedule = schedule;
                    OverlapWarning = string.Format(CultureInfo.CurrentCulture,
                        Resources.EventOverlapWarningFormat, conflict.Title,
                        conflict.StartsAtUtc.ToLocalTime(), conflict.EndsAtUtc.ToLocalTime());
                    OnPropertyChanged(nameof(SaveButtonLabel));
                    return;
                }
            }

            ClearOverlapWarning();
            CalendarEventResponse saved = editedEvent is { } current
                ? await eventClient.UpdateAsync(current.Id, model!, cancellationToken)
                : await eventClient.CreateAsync(model!, cancellationToken);
            IsEditing = false;
            if (saved.Recurrence == EventRecurrenceCode.None)
            {
                selectedDate = saved.StartsAtUtc.ToLocalTime().Date;
            }
            else
            {
                selectedDate = FindNextRecurringDate(saved, SelectedDate ?? DateTime.Today);
            }

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

    private void ClearOverlapWarning()
    {
        if (pendingOverlapSchedule is null && overlapWarning.Length == 0)
        {
            return;
        }

        pendingOverlapSchedule = null;
        OverlapWarning = string.Empty;
        OnPropertyChanged(nameof(SaveButtonLabel));
    }

    private bool CanSaveEvent()
    {
        if (!IsEditing || IsSaving || string.IsNullOrWhiteSpace(EventTitle) || EventTitle.Length > 500 ||
            string.IsNullOrWhiteSpace(EventDescription) || EventDescription.Length > 2000 ||
            SelectedColor is null || SelectedRecurrence is null)
        {
            return false;
        }

        return IsRecurringEvent
            ? SelectedDayOfMonth is >= 1 and <= 31 &&
              (!IsYearlyEvent || SelectedMonthOfYear is >= 1 and <= 12 &&
                  SelectedDayOfMonth <= DateTime.DaysInMonth(2024, SelectedMonthOfYear)) &&
              TryGetRecurringWindow(out _, out _, out _)
            : StartDate is not null && EndDate is not null && SelectedReminder is not null &&
              SelectedEmailReminder is not null &&
              TimeOnly.TryParse(StartTime, CultureInfo.CurrentCulture, out _) &&
              TimeOnly.TryParse(EndTime, CultureInfo.CurrentCulture, out _);
    }

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
            SelectedColor is null || SelectedRecurrence is null ||
            (!IsRecurringEvent && (SelectedReminder is null || SelectedEmailReminder is null)))
        {
            EditorError = Resources.InvalidEventData;
            return false;
        }

        if (!TimeOnly.TryParse(StartTime, CultureInfo.CurrentCulture, out TimeOnly startTimeValue) ||
            !TimeOnly.TryParse(EndTime, CultureInfo.CurrentCulture, out TimeOnly endTimeValue))
        {
            EditorError = Resources.EventDateTimeRequired;
            return false;
        }

        DateTime startDateValue;
        DateTime endDateValue;
        int? recurrenceMonth = null;
        int? recurrenceDay = null;
        int? intervalMinutes = null;
        if (IsRecurringEvent)
        {
            if (!TryGetRecurringWindow(out _, out _, out int interval) ||
                SelectedDayOfMonth is < 1 or > 31 ||
                (IsYearlyEvent && (SelectedMonthOfYear is < 1 or > 12 ||
                    SelectedDayOfMonth > DateTime.DaysInMonth(2024, SelectedMonthOfYear))))
            {
                EditorError = Resources.NotificationWindowInvalid;
                return false;
            }

            startDateValue = IsYearlyEvent
                ? GetYearlyAnchor(SelectedMonthOfYear, SelectedDayOfMonth, startTimeValue, endTimeValue)
                : GetMonthlyAnchor(SelectedDayOfMonth, startTimeValue, endTimeValue);
            endDateValue = startDateValue;
            recurrenceMonth = IsYearlyEvent ? SelectedMonthOfYear : null;
            recurrenceDay = SelectedDayOfMonth;
            intervalMinutes = interval;
        }
        else
        {
            if (StartDate is not { } startDate || EndDate is not { } endDate)
            {
                EditorError = Resources.EventDateTimeRequired;
                return false;
            }

            startDateValue = startDate.Date;
            endDateValue = endDate.Date;
        }

        DateTime localStart = startDateValue.Add(startTimeValue.ToTimeSpan());
        DateTime localEnd = endDateValue.Add(endTimeValue.ToTimeSpan());
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

        List<ReminderRuleDto> reminders = IsRecurringEvent ? [] :
            editedEvent?.Reminders.Where(rule =>
                (!reminderChanged || !IsSimpleDesktopReminder(rule)) &&
                (!emailReminderChanged || !IsSimpleEmailReminder(rule))).ToList() ?? [];
        if (!IsRecurringEvent && (editedEvent is null || reminderChanged) &&
            SelectedReminder is { Minutes: > 0 } reminder)
        {
            reminders.Add(new ReminderRuleDto(TimeSpan.FromMinutes(reminder.Minutes), ReminderChannelCode.Desktop, 1, null));
        }
        if (!IsRecurringEvent && (editedEvent is null || emailReminderChanged) &&
            SelectedEmailReminder is { Minutes: > 0 } emailReminder)
        {
            reminders.Add(new ReminderRuleDto(TimeSpan.FromMinutes(emailReminder.Minutes), ReminderChannelCode.Email, 1, null));
        }

        model = new SaveEventDto(EventTitle.Trim(), ToLocalOffset(localStart), ToLocalOffset(localEnd),
            TimeZoneInfo.Local.Id, IsEnabled, reminders, EventDescription.Trim(), SelectedColor.ColorHex,
            SelectedRecurrence.Code, recurrenceMonth, recurrenceDay, intervalMinutes);
        EditorError = string.Empty;
        return true;
    }

    private static bool IsSimpleDesktopReminder(ReminderRuleDto rule) =>
        rule.Channel == ReminderChannelCode.Desktop && rule.OccurrenceCount == 1 && rule.RepeatInterval is null;

    private static bool IsSimpleEmailReminder(ReminderRuleDto rule) =>
        rule.Channel == ReminderChannelCode.Email && rule.OccurrenceCount == 1 && rule.RepeatInterval is null;

    private static DateTime GetMonthlyAnchor(int day, TimeOnly first, TimeOnly last)
    {
        DateTime month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (int offset = 0; offset < 24; offset++)
        {
            DateTime candidateMonth = month.AddMonths(offset);
            if (day > DateTime.DaysInMonth(candidateMonth.Year, candidateMonth.Month))
            {
                continue;
            }

            DateTime date = new(candidateMonth.Year, candidateMonth.Month, day);
            if (!TimeZoneInfo.Local.IsInvalidTime(date.Add(first.ToTimeSpan())) &&
                !TimeZoneInfo.Local.IsInvalidTime(date.Add(last.ToTimeSpan())))
            {
                return date;
            }
        }

        throw new InvalidOperationException(DomainValidationMessages.EventNotificationWindowInvalid);
    }

    private static DateTime FindNextRecurringDate(CalendarEventResponse calendarEvent, DateTime from)
    {
        for (DateTime date = from.Date; date < from.Date.AddYears(8); date = date.AddDays(1))
        {
            if (calendarEvent.Recurrence == EventRecurrenceCode.Yearly &&
                date.Month == calendarEvent.RecurrenceMonth && date.Day == calendarEvent.RecurrenceDay ||
                calendarEvent.Recurrence == EventRecurrenceCode.Monthly &&
                date.Day == calendarEvent.RecurrenceDay)
            {
                return date;
            }
        }

        return from.Date;
    }

    private static DateTime GetYearlyAnchor(int month, int day, TimeOnly first, TimeOnly last)
    {
        for (int year = DateTime.Today.Year; year < DateTime.Today.Year + 8; year++)
        {
            if (day > DateTime.DaysInMonth(year, month))
            {
                continue;
            }

            DateTime date = new(year, month, day);
            if (!TimeZoneInfo.Local.IsInvalidTime(date.Add(first.ToTimeSpan())) &&
                !TimeZoneInfo.Local.IsInvalidTime(date.Add(last.ToTimeSpan())))
            {
                return date;
            }
        }

        throw new InvalidOperationException(DomainValidationMessages.EventNotificationWindowInvalid);
    }

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

    private readonly record struct OverlapSchedule(
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        EventRecurrenceCode Recurrence,
        int? RecurrenceMonth,
        int? RecurrenceDay,
        int? NotificationIntervalMinutes,
        bool IsEnabled)
    {
        public static OverlapSchedule From(SaveEventDto model) => new(
            model.StartsAt, model.EndsAt, model.Recurrence, model.RecurrenceMonth,
            model.RecurrenceDay, model.NotificationIntervalMinutes, model.IsEnabled);
    }
}
