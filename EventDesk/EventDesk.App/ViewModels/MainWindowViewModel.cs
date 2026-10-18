using CommunityToolkit.Mvvm.ComponentModel;
using EventDesk.App.Views.Pages;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Icons;
using Wpf.Ui.Controls;

namespace EventDesk.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private string maximizeRestoreIconPath;
    private string maximizeRestoreToolTip;

    public MainWindowViewModel(IResourceWrapper resourceWrapper)
    {
        ResourceWrapper = resourceWrapper ?? throw new ArgumentNullException(nameof(resourceWrapper));
        maximizeRestoreIconPath = resourceWrapper.MaximizeIconPath;
        maximizeRestoreToolTip = resourceWrapper.MaximizeToolTip;

        MenuItems =
        [
            new NavigationViewItem
            {
                Content = resourceWrapper.Calendar,
                Icon = NavigationIcons.FromImage(resourceWrapper.MenuCalendarIconPath),
                ToolTip = resourceWrapper.Calendar,
                TargetPageType = typeof(CalendarPage)
            },
            new NavigationViewItem
            {
                Content = resourceWrapper.History,
                Icon = NavigationIcons.FromImage(resourceWrapper.MenuHistoryIconPath),
                ToolTip = resourceWrapper.History,
                TargetPageType = typeof(HistoryPage)
            }
        ];
        FooterMenuItems =
        [
            new NavigationViewItem
            {
                Content = resourceWrapper.Settings,
                Icon = NavigationIcons.FromImage(resourceWrapper.MenuSettingsIconPath),
                ToolTip = resourceWrapper.Settings,
                TargetPageType = typeof(SettingsPage)
            },
            new NavigationViewItem
            {
                Content = resourceWrapper.About,
                Icon = NavigationIcons.FromImage(resourceWrapper.MenuAboutIconPath),
                ToolTip = resourceWrapper.About,
                TargetPageType = typeof(AboutPage)
            }
        ];
    }

    public IResourceWrapper ResourceWrapper { get; }

    public string Title => ResourceWrapper.AppTitle;

    public IReadOnlyList<object> MenuItems { get; }

    public IReadOnlyList<object> FooterMenuItems { get; }

    public string MaximizeRestoreIconPath
    {
        get => maximizeRestoreIconPath;
        private set => SetProperty(ref maximizeRestoreIconPath, value);
    }

    public string MaximizeRestoreToolTip
    {
        get => maximizeRestoreToolTip;
        private set => SetProperty(ref maximizeRestoreToolTip, value);
    }

    public void UpdateMaximizeRestoreState(bool isMaximized)
    {
        MaximizeRestoreIconPath = isMaximized
                                               ? ResourceWrapper.RestoreIconPath
                                               : ResourceWrapper.MaximizeIconPath;
        MaximizeRestoreToolTip = isMaximized
                                            ? ResourceWrapper.RestoreToolTip
                                            : ResourceWrapper.MaximizeToolTip;
    }
}
