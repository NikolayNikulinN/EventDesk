using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class HistoryViewModel
{
    public HistoryViewModel(IResourceWrapper resourceWrapper)
    {
        ArgumentNullException.ThrowIfNull(resourceWrapper);
        Title = resourceWrapper.History;
    }

    public string Title { get; }
}
