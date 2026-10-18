using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace EventDesk.App.Views.Pages;

public partial class SettingsPage : Page, INavigationAware
{
    private readonly SettingsViewModel viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    public Task OnNavigatedToAsync() => viewModel.LoadAsync();

    public Task OnNavigatedFromAsync()
    {
        PasswordInput.Clear();
        return Task.CompletedTask;
    }

    private async void SaveClick(object sender, System.Windows.RoutedEventArgs e)
    {
        string password = PasswordInput.Password;
        if (await viewModel.SaveAsync(password)) PasswordInput.Clear();
    }

    private void PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel?.SetPasswordState(
            PasswordInput.Password.Length,
            !string.IsNullOrWhiteSpace(PasswordInput.Password));
    }
}
