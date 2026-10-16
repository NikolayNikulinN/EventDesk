using CommunityToolkit.Mvvm.ComponentModel;
using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private string _maximizeRestoreIconPath;
    private string _maximizeRestoreToolTip;

    public MainWindowViewModel(IResourceWrapper resourceWrapper)
    {
        ResourceWrapper = resourceWrapper ?? throw new ArgumentNullException(nameof(resourceWrapper));
        _maximizeRestoreIconPath = resourceWrapper.MaximizeIconPath;
        _maximizeRestoreToolTip = resourceWrapper.MaximizeToolTip;
    }

    public IResourceWrapper ResourceWrapper { get; }

    public string Title => ResourceWrapper.AppTitle;

    public string MaximizeRestoreIconPath
    {
        get => _maximizeRestoreIconPath;
        private set => SetProperty(ref _maximizeRestoreIconPath, value);
    }

    public string MaximizeRestoreToolTip
    {
        get => _maximizeRestoreToolTip;
        private set => SetProperty(ref _maximizeRestoreToolTip, value);
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
