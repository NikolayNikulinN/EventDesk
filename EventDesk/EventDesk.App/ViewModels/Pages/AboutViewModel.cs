using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class AboutViewModel
{
    public AboutViewModel(IResourceWrapper resourceWrapper)
    {
        ArgumentNullException.ThrowIfNull(resourceWrapper);
        Title = resourceWrapper.About;
    }

    public string Title { get; }
}
