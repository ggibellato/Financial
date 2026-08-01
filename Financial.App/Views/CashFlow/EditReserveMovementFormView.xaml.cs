using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.Input;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class EditReserveMovementFormView : UserControl
{
    public EditReserveMovementFormView()
    {
        InitializeComponent();
    }

    private void OnAmountTextBoxLostFocus(object sender, RoutedEventArgs e)
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
