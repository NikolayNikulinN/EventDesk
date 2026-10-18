using EventDesk.App.Models;
using EventDesk.App.Services;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.ViewModels;
using EventDesk.App.Views;
using EventDesk.App.Views.Dialogs;
using EventDesk.App.Views.Pages;
using EventDesk.Contracts.Events;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Resources.Wrappers;
using EventDesk.Resources.Controls.Calendar;
using EventDesk.Resources.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using Xunit;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EventDesk.App.Tests;

public sealed class CalendarViewModelTests
{
    [Fact]
    public async Task Commands_CreateUpdateDisableAndDelete_RefreshCalendar()
    {
        // Arrange
        InMemoryEventClient client = new();
        CalendarViewModel viewModel = new(client, new ResourceWrapper(), new NullLogger());
        await viewModel.RefreshCommand.ExecuteAsync(null);

        // Act: create
        viewModel.NewEventCommand.Execute(null);
        viewModel.EventTitle = "Planning";
        viewModel.EventDescription = "Planning reminder";
        viewModel.StartTime = "09:00";
        viewModel.EndTime = "10:00";
        viewModel.SelectedReminder = viewModel.ReminderOptions[2];
        viewModel.SelectedColor = viewModel.ColorOptions[1];
        await viewModel.SaveEventCommand.ExecuteAsync(null);

        // Assert
        CalendarEventItem created = Assert.Single(viewModel.Days.SelectMany(day => day.Events));
        Assert.Equal("Planning", created.Title);
        Assert.Equal("Planning reminder", created.Source.Description);
        Assert.Equal(viewModel.ColorOptions[1].ColorHex, created.Source.ColorHex);
        Assert.Equal(15, Assert.Single(created.Source.Reminders).LeadTime.TotalMinutes);

        // Act: edit and disable
        viewModel.EditEventCommand.Execute(created);
        viewModel.EventTitle = "Updated planning";
        viewModel.EventDescription = "Updated reminder";
        viewModel.SelectedColor = viewModel.ColorOptions[2];
        viewModel.IsEnabled = false;
        await viewModel.SaveEventCommand.ExecuteAsync(null);

        // Assert
        CalendarEventItem updated = Assert.Single(viewModel.Days.SelectMany(day => day.Events));
        Assert.Equal("Updated planning", updated.Title);
        Assert.Equal("Updated reminder", updated.Source.Description);
        Assert.Equal(viewModel.ColorOptions[2].ColorHex, updated.Source.ColorHex);
        Assert.False(updated.IsEnabled);

        // Act: delete
        viewModel.EditEventCommand.Execute(updated);
        viewModel.RequestDeleteCommand.Execute(null);
        await viewModel.DeleteEventCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(viewModel.Days.SelectMany(day => day.Events));
        Assert.False(viewModel.IsEditing);
    }

    [Fact]
    public async Task SaveEvent_InvalidTimeRange_ShowsValidationAndSkipsRequest()
    {
        InMemoryEventClient client = new();
        CalendarViewModel viewModel = new(client, new ResourceWrapper(), new NullLogger());
        viewModel.NewEventCommand.Execute(null);
        viewModel.EventTitle = "Planning";
        viewModel.EventDescription = "Planning reminder";
        viewModel.StartTime = "11:00";
        viewModel.EndTime = "10:00";

        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEditing);
        Assert.NotEmpty(viewModel.EditorError);
        Assert.Empty(client.Events);
    }

    [Fact]
    public async Task SaveEvent_OverlappingEvent_WarnsThenAllowsExplicitSave()
    {
        InMemoryEventClient client = new();
        client.Events.Add(ExistingEvent("Existing meeting", DateTime.Today.AddHours(9), DateTime.Today.AddHours(10)));
        ResourceWrapper resources = new();
        CalendarViewModel viewModel = new(client, resources, new NullLogger());
        viewModel.NewEventCommand.Execute(null);
        viewModel.EventTitle = "Another meeting";
        viewModel.EventDescription = "Reminder";
        viewModel.StartTime = "09:30";
        viewModel.EndTime = "10:30";

        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEditing);
        Assert.Single(client.Events);
        Assert.Contains("Existing meeting", viewModel.OverlapWarning);
        Assert.Equal(resources.SaveAnyway, viewModel.SaveButtonLabel);

        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEditing);
        Assert.Equal(2, client.Events.Count);
    }

    [Fact]
    public async Task SaveEvent_ChangingTimeAfterWarning_RechecksOverlap()
    {
        InMemoryEventClient client = new();
        client.Events.Add(ExistingEvent("Existing meeting", DateTime.Today.AddHours(9), DateTime.Today.AddHours(10)));
        CalendarViewModel viewModel = new(client, new ResourceWrapper(), new NullLogger());
        viewModel.NewEventCommand.Execute(null);
        viewModel.EventTitle = "Another meeting";
        viewModel.EventDescription = "Reminder";
        viewModel.StartTime = "09:30";
        viewModel.EndTime = "10:30";
        await viewModel.SaveEventCommand.ExecuteAsync(null);

        viewModel.StartTime = "10:00";
        viewModel.EndTime = "11:00";
        Assert.Empty(viewModel.OverlapWarning);
        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEditing);
        Assert.Equal(2, client.Events.Count);
    }

    [Fact]
    public async Task SaveEvent_EditingSameEvent_DoesNotWarnAboutItself()
    {
        InMemoryEventClient client = new();
        client.Events.Add(ExistingEvent("Existing meeting", DateTime.Today.AddHours(9), DateTime.Today.AddHours(10)));
        CalendarViewModel viewModel = new(client, new ResourceWrapper(), new NullLogger());
        await viewModel.RefreshCommand.ExecuteAsync(null);
        viewModel.EditEventCommand.Execute(Assert.Single(viewModel.Days.SelectMany(day => day.Events)));

        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEditing);
        Assert.Empty(viewModel.OverlapWarning);
        Assert.Single(client.Events);
    }

    [Fact]
    public async Task SaveEvent_MonthlyRecurrence_WarnsAboutExistingEventOnOccurrenceDate()
    {
        InMemoryEventClient client = new();
        client.Events.Add(ExistingEvent("Existing meeting", DateTime.Today.AddHours(12), DateTime.Today.AddHours(13)));
        CalendarViewModel viewModel = new(client, new ResourceWrapper(), new NullLogger());
        viewModel.NewEventCommand.Execute(null);
        viewModel.EventTitle = "Monthly event";
        viewModel.EventDescription = "Reminder";
        viewModel.SelectedRecurrence = viewModel.RecurrenceOptions.Single(option => option.Code == EventRecurrenceCode.Monthly);
        viewModel.SelectedDayOfMonth = DateTime.Today.Day;

        await viewModel.SaveEventCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEditing);
        Assert.Contains("Existing meeting", viewModel.OverlapWarning);
        Assert.Single(client.Events);
    }

    [Fact]
    public void SaveEvent_RequiresTitleDescriptionAndDates_TracksSliderTime()
    {
        CalendarViewModel viewModel = new(new InMemoryEventClient(), new ResourceWrapper(), new NullLogger());
        viewModel.NewEventCommand.Execute(null);

        Assert.False(viewModel.SaveEventCommand.CanExecute(null));
        viewModel.EventTitle = "Planning";
        Assert.False(viewModel.SaveEventCommand.CanExecute(null));
        viewModel.EventDescription = "Planning reminder";
        Assert.True(viewModel.SaveEventCommand.CanExecute(null));

        viewModel.StartDate = null;
        Assert.False(viewModel.SaveEventCommand.CanExecute(null));
        viewModel.StartDate = DateTime.Today;
        viewModel.StartMinuteOfDay = 9 * 60 + 30;
        Assert.Equal("09:30", viewModel.StartTime);
        Assert.True(viewModel.SaveEventCommand.CanExecute(null));
    }

    [Fact]
    public async Task RecurringEditor_SavesYearlessAndMonthlessSchedules()
    {
        InMemoryEventClient client = new();
        CalendarViewModel yearly = new(client, new ResourceWrapper(), new NullLogger());
        yearly.NewEventCommand.Execute(null);
        yearly.EventTitle = "Birthday";
        yearly.EventDescription = "Birthday notification";
        yearly.SelectedRecurrence = yearly.RecurrenceOptions.Single(option => option.Code == EventRecurrenceCode.Yearly);
        yearly.SelectedMonthOfYear = 2;
        yearly.SelectedDayOfMonth = 29;

        Assert.Equal("11:00, 14:00, 17:00", yearly.NotificationTimesPreview);
        Assert.True(yearly.SaveEventCommand.CanExecute(null));
        await yearly.SaveEventCommand.ExecuteAsync(null);
        CalendarEventResponse birthday = Assert.Single(client.Events);
        Assert.Equal(EventRecurrenceCode.Yearly, birthday.Recurrence);
        Assert.Equal(2, birthday.RecurrenceMonth);
        Assert.Equal(29, birthday.RecurrenceDay);
        Assert.Equal(180, birthday.NotificationIntervalMinutes);
        Assert.Equal(2, yearly.SelectedDate!.Value.Month);
        Assert.Equal(29, yearly.SelectedDate.Value.Day);

        CalendarViewModel monthly = new(client, new ResourceWrapper(), new NullLogger());
        monthly.NewEventCommand.Execute(null);
        monthly.EventTitle = "Month end";
        monthly.EventDescription = "Month end notification";
        monthly.SelectedRecurrence = monthly.RecurrenceOptions.Single(option => option.Code == EventRecurrenceCode.Monthly);
        monthly.SelectedDayOfMonth = 31;
        await monthly.SaveEventCommand.ExecuteAsync(null);
        CalendarEventResponse monthEnd = Assert.Single(client.Events, item => item.Title == "Month end");
        Assert.Equal(EventRecurrenceCode.Monthly, monthEnd.Recurrence);
        Assert.Null(monthEnd.RecurrenceMonth);
        Assert.Equal(31, monthEnd.RecurrenceDay);
        Assert.Equal(31, monthly.SelectedDate!.Value.Day);
    }

    [Fact]
    public async Task NextWeek_ChangesVisibleDatesAndReloadsEvents()
    {
        CalendarViewModel viewModel = new(new InMemoryEventClient(), new ResourceWrapper(), new NullLogger());
        await viewModel.RefreshCommand.ExecuteAsync(null);
        DateTime firstDay = viewModel.Days[0].Date;

        viewModel.NextWeekCommand.Execute(null);
        await viewModel.RefreshCommand.ExecutionTask!;

        Assert.Equal(firstDay.AddDays(7), viewModel.Days[0].Date);
    }

    [Fact]
    public async Task Refresh_ServiceUnavailable_ShowsLocalizedError()
    {
        InMemoryEventClient client = new() { FailOnRead = true };
        ResourceWrapper resources = new();
        CalendarViewModel viewModel = new(client, resources, new NullLogger());

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(resources.ServiceUnavailable, viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task CalendarPage_AtDesktopWidth_ShowsFullWeekAndLoadsResources()
    {
        TaskCompletionSource<Exception?> completion = new();
        Thread thread = new(() =>
        {
            try
            {
                Application application = new();
                InteractiveCursorBehavior.Register();
                System.Windows.Controls.Button templateButton = new() { Cursor = Cursors.Arrow };
                templateButton.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.Equal(Cursors.Hand, templateButton.Cursor);
                ResourceDictionary resources = (ResourceDictionary)Application.LoadComponent(
                    new Uri("/EventDesk.Resources;component/Resources.xaml", UriKind.Relative));
                application.Resources.MergedDictionaries.Add(resources);
                CalendarViewModel viewModel = new(new InMemoryEventClient(), new ResourceWrapper(), new NullLogger());
                DateTime monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
                for (int index = 0; index < 7; index++)
                {
                    CalendarDay day = new(monday.AddDays(index));
                    if (day.Date == DateTime.Today)
                    {
                        DateTimeOffset start = new(day.Date.AddHours(9), TimeZoneInfo.Local.GetUtcOffset(day.Date.AddHours(9)));
                        DateTimeOffset end = start.AddHours(1);
                        day.Events.Add(new CalendarEventItem(new CalendarEventResponse(
                            Guid.NewGuid(), "Planning", start.ToUniversalTime(), end.ToUniversalTime(),
                            TimeZoneInfo.Local.Id, true, []), day.Date, viewModel.Resources.EventTimeRangeFormat));
                    }

                    viewModel.Days.Add(day);
                }

                viewModel.NewEventCommand.Execute(null);
                CalendarPage page = new(viewModel);
                Assert.False(ScrollViewer.GetCanContentScroll(page));
                page.Measure(new Size(1200, 700));
                page.Arrange(new Rect(0, 0, 1200, 700));
                page.UpdateLayout();
                Assert.Empty(FindVisualChildren<CalendarEditorControl>(page));
                Assert.Single(FindVisualChildren<MiniCalendarControl>(page));
                CalendarEditorDialog dialog = new(viewModel);
                Assert.Same(viewModel, dialog.DataContext);
                Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
                Assert.False(dialog.ShowInTaskbar);
                Grid dialogContent = Assert.IsType<Grid>(dialog.Content);
                CalendarEditorControl editor = Assert.Single(dialogContent.Children.OfType<CalendarEditorControl>());
                System.Windows.Controls.Button backdrop =
                    Assert.Single(dialogContent.Children.OfType<System.Windows.Controls.Button>());
                dialogContent.Measure(new Size(720, 460));
                dialogContent.Arrange(new Rect(0, 0, 720, 460));
                dialogContent.UpdateLayout();
                Assert.Same(viewModel.CancelEditCommand, backdrop.Command);
                Assert.InRange(editor.ActualHeight, 1, dialogContent.ActualHeight);
                Assert.Contains(FindVisualChildren<System.Windows.Controls.TextBlock>(editor),
                    label => label.Text == viewModel.Resources.NewEvent);
                page.Measure(new Size(720, 460));
                page.Arrange(new Rect(0, 0, 720, 460));
                page.UpdateLayout();
                page.Measure(new Size(1200, 700));
                page.Arrange(new Rect(0, 0, 1200, 700));
                page.UpdateLayout();
                WeekCalendarControl week = (WeekCalendarControl)page.FindName("WeekCalendar")!;
                WrapPanel toolbar = (WrapPanel)page.FindName("CalendarToolbar")!;
                string[] iconNames = ["previousWeek.png", "today.png", "nextWeek.png", "updateEvent.png", "addEvent.png"];
                System.Windows.Controls.Button[] toolbarButtons = toolbar.Children.Cast<System.Windows.Controls.Button>().ToArray();
                Assert.Equal(iconNames.Length, toolbarButtons.Length);
                for (int index = 0; index < toolbarButtons.Length; index++)
                {
                    System.Windows.Controls.Button button = toolbarButtons[index];
                    System.Windows.Controls.Image icon = Assert.IsType<System.Windows.Controls.Image>(button.Content);
                    Assert.EndsWith(iconNames[index], Assert.IsAssignableFrom<ImageSource>(icon.Source).ToString());
                    Assert.False(string.IsNullOrWhiteSpace(button.ToolTip?.ToString()));
                    Assert.False(string.IsNullOrWhiteSpace(System.Windows.Automation.AutomationProperties.GetName(button)));
                }
                Point toolbarBottomRight = toolbar.TranslatePoint(new Point(toolbar.ActualWidth, toolbar.ActualHeight), page);
                Point weekTopRight = week.TranslatePoint(new Point(week.ActualWidth, 0), page);
                Assert.Equal(weekTopRight.X, toolbarBottomRight.X, 1);
                Assert.InRange(weekTopRight.Y - toolbarBottomRight.Y, 7, 16);
                System.Windows.Controls.ScrollViewer timeline =
                    (System.Windows.Controls.ScrollViewer)week.FindName("TimelineScrollViewer")!;
                System.Windows.Controls.ScrollViewer header =
                    (System.Windows.Controls.ScrollViewer)week.FindName("HeaderScrollViewer")!;
                double headerTop = header.TransformToAncestor(week).Transform(new Point()).Y;
                timeline.ScrollToVerticalOffset(timeline.VerticalOffset + 100);
                page.UpdateLayout();
                Assert.Equal(headerTop, header.TransformToAncestor(week).Transform(new Point()).Y, 1);
                double narrowDayWidth = week.DayWidth;
                double shortHourHeight = week.HourHeight;
                Assert.True(timeline.ExtentWidth <= timeline.ViewportWidth + 1);
                viewModel.CancelEditCommand.Execute(null);
                page.Measure(new Size(1200, 700));
                page.Arrange(new Rect(0, 0, 1200, 700));
                page.UpdateLayout();
                Assert.False(viewModel.IsEditing);
                Assert.NotNull(page);
                Assert.True(timeline.ExtentWidth <= timeline.ViewportWidth + 1);
                Assert.Equal(narrowDayWidth, week.DayWidth, 1);
                foreach (ButtonBase button in FindVisualChildren<ButtonBase>(page))
                {
                    button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }
                Assert.All(FindVisualChildren<ButtonBase>(page).Where(button => button.IsEnabled),
                    button => Assert.True(button.Cursor == Cursors.Hand, $"{button.GetType().Name}: {button.Name}"));
                Assert.All(FindVisualChildren<CalendarDayButton>(page), button => Assert.Null(button.FocusVisualStyle));

                page.Measure(new Size(1800, 1300));
                page.Arrange(new Rect(0, 0, 1800, 1300));
                page.UpdateLayout();
                Assert.True(week.DayWidth > narrowDayWidth + 20);
                Assert.True(week.HourHeight > shortHourHeight + 5);
                Assert.True(timeline.ExtentWidth <= timeline.ViewportWidth + 1);
                DayColumnControl[] columns = FindVisualChildren<DayColumnControl>(week).ToArray();
                Assert.Equal(7, columns.Length);
                Assert.All(columns, column => Assert.InRange(column.ActualWidth, week.DayWidth - 1, week.DayWidth + 1));
                EventCardControl card = Assert.Single(FindVisualChildren<EventCardControl>(week));
                Assert.True(card.ActualWidth > CalendarLayout.DayWidth);

                double wideDayWidth = week.DayWidth;
                double tallHourHeight = week.HourHeight;
                page.Measure(new Size(930, 700));
                page.Arrange(new Rect(0, 0, 930, 700));
                page.UpdateLayout();
                Assert.True(toolbar.ActualWidth <= week.ActualWidth + 1);
                Assert.True(timeline.ScrollableWidth > 50);
                timeline.ScrollToHorizontalOffset(50);
                page.UpdateLayout();
                Assert.Equal(timeline.HorizontalOffset, header.HorizontalOffset, 1);
                Assert.Equal(timeline.ViewportWidth, header.ViewportWidth, 1);
                header.ScrollToHorizontalOffset(80);
                page.UpdateLayout();
                Assert.Equal(header.HorizontalOffset, timeline.HorizontalOffset, 1);
                page.Measure(new Size(1200, 700));
                page.Arrange(new Rect(0, 0, 1200, 700));
                page.UpdateLayout();
                Assert.True(week.DayWidth < wideDayWidth);
                Assert.True(week.HourHeight < tallHourHeight);
                Assert.NotNull(new MiniCalendarControl());
                Assert.NotNull(new WeekCalendarControl());
                Assert.NotNull(new DayColumnControl());
                Assert.NotNull(new EventCardControl());
                Assert.NotNull(new CalendarEditorControl());
                ServiceCollection services = new();
                services.AddNavigationViewPageProvider();
                services.AddSingleton<INavigationService, NavigationService>();
                using ServiceProvider provider = services.BuildServiceProvider();
                MainWindowViewModel shellViewModel = new(new ResourceWrapper());
                MainWindow shell = new(shellViewModel,
                    provider.GetRequiredService<INavigationService>());
                shell.Measure(new Size(1280, 800));
                shell.Arrange(new Rect(0, 0, 1280, 800));
                shell.UpdateLayout();
                NavigationView navigation = (NavigationView)shell.FindName("RootNavigationView")!;
                Assert.False(navigation.IsPaneOpen);
                Assert.Equal(NavigationViewBackButtonVisible.Collapsed, navigation.IsBackButtonVisible);
                NavigationViewItem[] menuItems = shellViewModel.MenuItems
                    .Concat(shellViewModel.FooterMenuItems)
                    .Cast<NavigationViewItem>()
                    .ToArray();
                string[] menuIconNames = ["menuCalendar.png", "menuHistory.png", "menuSettings.png", "menuAbout.png"];
                Assert.Equal(menuIconNames.Length, menuItems.Length);
                for (int index = 0; index < menuItems.Length; index++)
                {
                    ImageIcon icon = Assert.IsType<ImageIcon>(menuItems[index].Icon);
                    CroppedBitmap cropped = Assert.IsType<CroppedBitmap>(icon.Source);
                    BitmapImage source = Assert.IsType<BitmapImage>(cropped.Source);
                    Assert.EndsWith($"/Images/{menuIconNames[index]}", source.UriSource.AbsoluteUri);
                }
                foreach (ButtonBase button in FindVisualChildren<ButtonBase>(shell))
                {
                    button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }
                Assert.All(FindVisualChildren<ButtonBase>(shell).Where(button => button.IsEnabled),
                    button => Assert.True(button.Cursor == Cursors.Hand, $"{button.GetType().Name}: {button.Name}"));
                completion.SetResult(null);
            }
            catch (Exception exception)
            {
                completion.SetResult(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Exception? error = await completion.Task;

        Assert.Null(error);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static CalendarEventResponse ExistingEvent(string title, DateTime start, DateTime end) => new(
        Guid.NewGuid(), title,
        new DateTimeOffset(start, TimeZoneInfo.Local.GetUtcOffset(start)).ToUniversalTime(),
        new DateTimeOffset(end, TimeZoneInfo.Local.GetUtcOffset(end)).ToUniversalTime(),
        TimeZoneInfo.Local.Id, true, [], "Reminder", "#425EBA");

    private sealed class InMemoryEventClient : ICalendarEventClient
    {
        public List<CalendarEventResponse> Events { get; } = [];

        public bool FailOnRead { get; set; }

        public Task<IReadOnlyList<CalendarEventResponse>> GetByPeriodAsync(
            DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            if (FailOnRead)
            {
                throw new EventDesk.Ipc.IpcException(
                    EventDesk.Contracts.Ipc.IpcErrorCode.TransportError, "service unavailable");
            }

            return Task.FromResult<IReadOnlyList<CalendarEventResponse>>(
                Events.Where(calendarEvent => calendarEvent.StartsAtUtc < to && calendarEvent.EndsAtUtc > from).ToArray());
        }

        public Task<CalendarEventResponse> CreateAsync(SaveEventDto calendarEvent, CancellationToken cancellationToken = default)
        {
            CalendarEventResponse created = ToResponse(Guid.NewGuid(), calendarEvent);
            Events.Add(created);
            return Task.FromResult(created);
        }

        public Task<CalendarEventResponse> UpdateAsync(Guid id, SaveEventDto calendarEvent, CancellationToken cancellationToken = default)
        {
            CalendarEventResponse updated = ToResponse(id, calendarEvent);
            Events[Events.FindIndex(item => item.Id == id)] = updated;
            return Task.FromResult(updated);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Events.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }

        private static CalendarEventResponse ToResponse(Guid id, SaveEventDto source) => new(
            id, source.Title, source.StartsAt.ToUniversalTime(), source.EndsAt.ToUniversalTime(),
            source.TimeZoneId, source.IsEnabled, source.Reminders, source.Description, source.ColorHex,
            source.Recurrence, source.RecurrenceMonth, source.RecurrenceDay, source.NotificationIntervalMinutes);
    }


    private sealed class NullLogger : ILoggerWrapper
    {
        public void Write(string message, params object?[] propertyValues) { }

        public void Write(string message, LogLevel level) { }

        public void Write(Exception? exception, LogLevel level, string messageTemplate, params object?[] propertyValues) { }

        public void CloseAndFlush() { }
    }
}
