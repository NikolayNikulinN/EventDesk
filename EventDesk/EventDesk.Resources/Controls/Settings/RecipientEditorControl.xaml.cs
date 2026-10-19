using System.Windows.Controls;

namespace EventDesk.Resources.Controls.Settings;

public partial class RecipientEditorControl : UserControl
{
    public RecipientEditorControl()
    {
        InitializeComponent();
        Loaded += (_, _) => RecipientAddressInput.Focus();
    }
}
