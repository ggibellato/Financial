using System.Globalization;
using System.Windows.Controls;

namespace Financial.Presentation.Shared.Input;

public static class DecimalInputHelper
{
    public static bool IsDecimalTextAllowed(TextBox textBox, string input)
    {
        var proposed = GetProposedText(textBox, input);
        return IsValidDecimalInput(proposed);
    }

    public static string GetProposedText(TextBox textBox, string input)
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

    public static bool IsValidDecimalInput(string text)
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

    public static string NormalizeDecimalSeparator(string text)
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        return separator == "."
            ? text.Replace(",", ".")
            : text.Replace(".", separator);
    }
}
