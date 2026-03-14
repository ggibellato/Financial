using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Financial.Presentation.Tools.ViewModels;

namespace Financial.Presentation.Tools;

public partial class CreditDialog : Window
{
    public CreditDialog(CreditDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        if (sender is CreditDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        DialogResult = dialogResult;
        Close();
    }

    private void OnDecimalTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.Handled = true;
            return;
        }

        e.Handled = !IsDecimalTextAllowed(textBox, e.Text);
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
        if (!IsDecimalTextAllowed(textBox, pasteText))
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

        var normalized = NormalizeDecimalSeparator(textBox.Text);
        if (!string.Equals(textBox.Text, normalized, StringComparison.Ordinal))
        {
            textBox.Text = normalized;
        }
    }

    private static bool IsDecimalTextAllowed(TextBox textBox, string input)
    {
        var proposed = GetProposedText(textBox, input);
        return IsValidDecimalInput(proposed);
    }

    private static string GetProposedText(TextBox textBox, string input)
    {
        var text = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;

        if (selectionLength > 0 && selectionStart + selectionLength <= text.Length)
        {
            text = text.Remove(selectionStart, selectionLength);
        }

        if (selectionStart < 0 || selectionStart > text.Length)
        {
            selectionStart = text.Length;
        }

        return text.Insert(selectionStart, input);
    }

    private static bool IsValidDecimalInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        int separatorCount = 0;
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
            {
                continue;
            }

            if (ch is '.' or ',')
            {
                separatorCount++;
                if (separatorCount > 1)
                {
                    return false;
                }

                continue;
            }

            return false;
        }

        return true;
    }

    private static string NormalizeDecimalSeparator(string text)
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        return separator == "."
            ? text.Replace(",", ".")
            : text.Replace(".", separator);
    }
}
