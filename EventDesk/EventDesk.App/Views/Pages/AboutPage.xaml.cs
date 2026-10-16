using System.Windows.Controls;
using EventDesk.App.ViewModels.Pages;

namespace EventDesk.App.Views.Pages;

public partial class AboutPage : Page
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
