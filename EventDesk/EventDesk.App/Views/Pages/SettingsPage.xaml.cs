using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.Views;
using EventDesk.Resources.Controls.Settings;
using Wpf.Ui.Abstractions.Controls;

namespace EventDesk.App.Views.Pages;

public partial class SettingsPage : Page, INavigationAware
{
    private readonly SettingsViewModel viewModel;
    private FrameworkElement? activeModal;
    private MainWindow? modalOwner;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    public Task OnNavigatedToAsync() => viewModel.LoadAsync();

    public Task OnNavigatedFromAsync()
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        HideModal();
        viewModel.CancelRecipientCommand.Execute(null);
        viewModel.CancelDeleteRecipientCommand.Execute(null);
        PasswordInput.Clear();
        return Task.CompletedTask;
    }

    private void PageLoaded(object sender, RoutedEventArgs e)
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        UpdateModal();
    }

    private void PageUnloaded(object sender, RoutedEventArgs e)
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        HideModal();
        viewModel.CancelRecipientCommand.Execute(null);
        viewModel.CancelDeleteRecipientCommand.Execute(null);
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsRecipientEditorOpen) or
            nameof(SettingsViewModel.IsDeleteConfirmationOpen))
        {
            UpdateModal();
        }
    }

    private void UpdateModal()
    {
        if (!IsLoaded || Window.GetWindow(this) is not MainWindow owner)
        {
            return;
        }

        if (viewModel.IsRecipientEditorOpen)
        {
            if (activeModal is RecipientEditorControl)
            {
                return;
            }

            HideModal();
            ShowModal(owner, new RecipientEditorControl { DataContext = viewModel });
        }
        else if (viewModel.IsDeleteConfirmationOpen)
        {
            if (activeModal is RecipientDeleteControl)
            {
                return;
            }

            HideModal();
            ShowModal(owner, new RecipientDeleteControl { DataContext = viewModel });
        }
        else
        {
            HideModal();
        }
    }

    private void ShowModal(MainWindow owner, FrameworkElement content)
    {
        activeModal = content;
        modalOwner = owner;
        owner.ShowModal(content);
    }

    private void HideModal()
    {
        if (activeModal is { } content)
        {
            modalOwner?.CloseModal(content);
            activeModal = null;
            modalOwner = null;
        }
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
