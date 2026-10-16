using System.Windows;
using EventDesk.App.ViewModels;

namespace EventDesk.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.UpdateMaximizeRestoreState(WindowState == WindowState.Maximized);
    }

    private void MinimizeButtonClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeRestoreButtonClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButtonClick(object sender, RoutedEventArgs e) => Close();

    private void WindowStateChanged(object sender, EventArgs e) =>
        _viewModel.UpdateMaximizeRestoreState(WindowState == WindowState.Maximized);
}
