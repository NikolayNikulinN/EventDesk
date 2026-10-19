using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.Views;
using EventDesk.Resources.Controls.Calendar;

namespace EventDesk.App.Views.Pages;

public partial class CalendarPage : Page
{
    private readonly CalendarViewModel viewModel;
    private CalendarEditorControl? editorControl;
    private MainWindow? editorOwner;
    private int miniCalendarAnimationVersion;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        UpdateMiniCalendar(animate: false);
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        await viewModel.RefreshCommand.ExecuteAsync(null);
        if (viewModel.IsEditing)
        {
            ShowEditorModal();
        }
    }

    private void PageUnloaded(object sender, RoutedEventArgs e)
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        UpdateMiniCalendar(animate: false);
        viewModel.RefreshCommand.Cancel();
        HideEditorModal();
        viewModel.CancelEditCommand.Execute(null);
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CalendarViewModel.IsMiniCalendarExpanded))
        {
            UpdateMiniCalendar(animate: true);
            return;
        }

        if (e.PropertyName != nameof(CalendarViewModel.IsEditing))
        {
            return;
        }

        if (viewModel.IsEditing)
        {
            ShowEditorModal();
        }
        else
        {
            HideEditorModal();
        }
    }

    private void UpdateMiniCalendar(bool animate)
    {
        bool expanded = viewModel.IsMiniCalendarExpanded;
        double targetWidth = expanded ? CalendarLayout.MiniCalendarWidth : 0;
        double targetOpacity = expanded ? 1 : 0;
        int version = ++miniCalendarAnimationVersion;

        if (!animate)
        {
            MiniCalendarHost.BeginAnimation(FrameworkElement.WidthProperty, null);
            MiniCalendarHost.BeginAnimation(UIElement.OpacityProperty, null);
            MiniCalendarHost.Width = targetWidth;
            MiniCalendarHost.Opacity = targetOpacity;
            MiniCalendarHost.ClipToBounds = !expanded;
            MiniCalendarHost.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        if (expanded)
        {
            MiniCalendarHost.Visibility = Visibility.Visible;
        }

        MiniCalendarHost.ClipToBounds = true;
        Duration duration = new(CalendarLayout.MiniCalendarTransitionDuration);
        CubicEase easing = new() { EasingMode = EasingMode.EaseInOut };
        DoubleAnimation widthAnimation = new()
        {
            From = MiniCalendarHost.ActualWidth,
            To = targetWidth,
            Duration = duration,
            EasingFunction = easing
        };
        DoubleAnimation opacityAnimation = new()
        {
            From = MiniCalendarHost.Opacity,
            To = targetOpacity,
            Duration = duration,
            EasingFunction = easing
        };
        widthAnimation.Completed += (_, _) =>
        {
            if (version != miniCalendarAnimationVersion)
            {
                return;
            }

            MiniCalendarHost.Width = targetWidth;
            MiniCalendarHost.Opacity = targetOpacity;
            MiniCalendarHost.BeginAnimation(FrameworkElement.WidthProperty, null);
            MiniCalendarHost.BeginAnimation(UIElement.OpacityProperty, null);
            MiniCalendarHost.ClipToBounds = !expanded;
            MiniCalendarHost.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        };
        MiniCalendarHost.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
        MiniCalendarHost.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
    }

    private void ShowEditorModal()
    {
        if (editorControl is not null || !IsLoaded || Window.GetWindow(this) is not MainWindow owner)
        {
            return;
        }

        CalendarEditorControl editor = new() { DataContext = viewModel };
        editorControl = editor;
        editorOwner = owner;
        owner.ShowModal(editor);
    }

    private void HideEditorModal()
    {
        if (editorControl is { } editor)
        {
            editorOwner?.CloseModal(editor);
            editorControl = null;
            editorOwner = null;
        }
    }
}
