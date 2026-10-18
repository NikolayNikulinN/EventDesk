using System.Windows;
using EventDesk.App.ViewModels;
using Wpf.Ui;

namespace EventDesk.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        DataContext = viewModel;

        Rect workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, workArea.Width);
        MinHeight = Math.Min(MinHeight, workArea.Height);
        Width = Math.Min(Width, workArea.Width);
        Height = Math.Min(Height, workArea.Height);

        navigationService.SetNavigationControl(RootNavigationView);
        viewModel.UpdateMaximizeRestoreState(WindowState == WindowState.Maximized);
    }

    private void MinimizeButtonClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeRestoreButtonClick(object sender, RoutedEventArgs e) =>
                                                                                 WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButtonClick(object sender, RoutedEventArgs e) => Close();

    private void WindowStateChanged(object sender, EventArgs e) => viewModel.UpdateMaximizeRestoreState(WindowState == WindowState.Maximized);
}
