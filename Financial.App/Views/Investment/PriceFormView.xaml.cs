using Financial.Presentation.App.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial.Presentation.App.Views.Investment;

public partial class PriceFormView : UserControl
{
    public PriceFormView()
    {
        InitializeComponent();
    }

    private void OnDecimalTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.Handled = true;
            return;
        }

        e.Handled = !DecimalInputHelper.IsDecimalTextAllowed(textBox, e.Text);
    }

    private void OnDecimalTextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.CancelCommand();
            return;
        }

        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!DecimalInputHelper.IsDecimalTextAllowed(textBox, pasteText))
        {
            e.CancelCommand();
        }
    }

    private void OnDecimalTextBoxLostFocus(object sender, RoutedEventArgs e)
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
