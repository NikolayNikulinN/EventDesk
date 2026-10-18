using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace EventDesk.App.Views.Pages;

public partial class HistoryPage : Page, INavigationAware
{
    private readonly HistoryViewModel viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    public Task OnNavigatedToAsync() => viewModel.LoadAsync();

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
