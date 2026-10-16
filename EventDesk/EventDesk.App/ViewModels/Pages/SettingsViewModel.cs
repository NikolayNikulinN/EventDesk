using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class SettingsViewModel
{
    public SettingsViewModel(IResourceWrapper resourceWrapper)
    {
        ArgumentNullException.ThrowIfNull(resourceWrapper);
        Title = resourceWrapper.Settings;
    }

    public string Title { get; }
}
