using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class CalendarViewModel
{
    public CalendarViewModel(IResourceWrapper resourceWrapper)
    {
        ArgumentNullException.ThrowIfNull(resourceWrapper);
        Title = resourceWrapper.Calendar;
    }

    public string Title { get; }
}
