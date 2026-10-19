using System.Windows;
using System.ComponentModel;
using EventDesk.App.ViewModels;
using Wpf.Ui;

namespace EventDesk.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private bool exiting;

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

    public void ShowModal(FrameworkElement content)
    {
        ArgumentNullException.ThrowIfNull(content);
        RootNavigationView.IsPaneOpen = false;
        ModalHost.Content = content;
        ShellContent.IsEnabled = false;
        ModalOverlay.Visibility = Visibility.Visible;
    }

    public void CloseModal(FrameworkElement content)
    {
        if (!ReferenceEquals(ModalHost.Content, content))
        {
            return;
        }

        ModalOverlay.Visibility = Visibility.Collapsed;
        ModalHost.Content = null;
        ShellContent.IsEnabled = true;
    }

    public void PrepareForExit() => exiting = true;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!exiting && ModalOverlay.Visibility == Visibility.Visible)
        {
            e.Cancel = true;
            base.OnClosing(e);
            return;
        }

        if (!exiting)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void WindowStateChanged(object sender, EventArgs e) => viewModel.UpdateMaximizeRestoreState(WindowState == WindowState.Maximized);
}
