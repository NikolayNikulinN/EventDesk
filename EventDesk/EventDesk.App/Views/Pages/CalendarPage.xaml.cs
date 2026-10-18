using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;
using EventDesk.App.Views.Dialogs;

namespace EventDesk.App.Views.Pages;

public partial class CalendarPage : Page
{
    private readonly CalendarViewModel viewModel;
    private CalendarEditorDialog? editorDialog;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
    }

    private async void PageLoaded(object sender, RoutedEventArgs e)
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        await viewModel.RefreshCommand.ExecuteAsync(null);
        if (viewModel.IsEditing)
        {
            ShowEditorDialog();
        }
    }

    private void PageUnloaded(object sender, RoutedEventArgs e)
    {
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        viewModel.RefreshCommand.Cancel();
        editorDialog?.Close();
        viewModel.CancelEditCommand.Execute(null);
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CalendarViewModel.IsEditing))
        {
            return;
        }

        if (viewModel.IsEditing)
        {
            ShowEditorDialog();
        }
        else
        {
            editorDialog?.Close();
        }
    }

    private void ShowEditorDialog()
    {
        if (editorDialog is not null || !IsLoaded || Window.GetWindow(this) is not { } owner)
        {
            return;
        }

        CalendarEditorDialog dialog = new(viewModel)
        {
            Owner = owner,
            Width = owner.ActualWidth,
            Height = owner.ActualHeight
        };
        editorDialog = dialog;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            editorDialog = null;
            if (viewModel.IsEditing)
            {
                viewModel.CancelEditCommand.Execute(null);
            }
        }
    }
}
