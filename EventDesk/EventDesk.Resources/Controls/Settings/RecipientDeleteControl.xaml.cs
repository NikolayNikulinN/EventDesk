using System.Windows.Controls;

namespace EventDesk.Resources.Controls.Settings;

public partial class RecipientDeleteControl : UserControl
{
    public RecipientDeleteControl()
    {
        InitializeComponent();
        Loaded += (_, _) => CancelDeleteButton.Focus();
    }
}
