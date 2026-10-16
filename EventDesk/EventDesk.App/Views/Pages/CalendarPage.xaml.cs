using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;

namespace EventDesk.App.Views.Pages;

public partial class CalendarPage : Page
{
    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
