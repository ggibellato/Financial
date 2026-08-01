using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.Input;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class CreateEntryFormView : UserControl
{
    public CreateEntryFormView()
    {
        InitializeComponent();
    }

    private void OnValueTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var normalized = DecimalInputHelper.NormalizeDecimalSeparator(textBox.Text);
        if (!string.Equals(textBox.Text, normalized, StringComparison.Ordinal))
        {
            textBox.Text = normalized;
        }
    }
}
