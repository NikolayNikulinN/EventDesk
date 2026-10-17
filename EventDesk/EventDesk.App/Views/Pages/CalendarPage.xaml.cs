using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;

namespace EventDesk.App.Views.Pages;

public partial class CalendarPage : Page
{
    private readonly CalendarViewModel viewModel;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    private async void PageLoaded(object sender, System.Windows.RoutedEventArgs e) =>
        await viewModel.RefreshCommand.ExecuteAsync(null);

    private void PageUnloaded(object sender, System.Windows.RoutedEventArgs e) =>
        viewModel.RefreshCommand.Cancel();
}
