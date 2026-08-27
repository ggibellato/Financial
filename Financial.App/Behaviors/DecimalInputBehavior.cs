using Financial.Presentation.App.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Masks a TextBox to accept only decimal input (typed or pasted) and normalizes the separator on
/// blur, via <see cref="DecimalInputHelper"/>. Replaces the same three event handlers that used to be
/// copy-pasted into every form's code-behind.
/// </summary>
public static class DecimalInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(DecimalInputBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>When true, a leading minus sign is accepted too - opt in per TextBox.</summary>
    public static readonly DependencyProperty AllowSignProperty = DependencyProperty.RegisterAttached(
        "AllowSign",
        typeof(bool),
        typeof(DecimalInputBehavior),
        new PropertyMetadata(false));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetAllowSign(DependencyObject element, bool value) => element.SetValue(AllowSignProperty, value);

    public static bool GetAllowSign(DependencyObject element) => (bool)element.GetValue(AllowSignProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        if (e.NewValue is true)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(textBox, OnPasting);
            textBox.LostFocus += OnLostFocus;
            return;
        }

        textBox.PreviewTextInput -= OnPreviewTextInput;
        DataObject.RemovePastingHandler(textBox, OnPasting);
        textBox.LostFocus -= OnLostFocus;
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.Handled = true;
            return;
        }

        e.Handled = !IsAllowed(textBox, e.Text);
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox || !e.SourceDataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!IsAllowed(textBox, pasteText))
        {
            e.CancelCommand();
        }
    }

    private static bool IsAllowed(TextBox textBox, string text) => GetAllowSign(textBox)
        ? DecimalInputHelper.IsSignedDecimalTextAllowed(textBox, text)
        : DecimalInputHelper.IsDecimalTextAllowed(textBox, text);

    private static void OnLostFocus(object sender, RoutedEventArgs e)
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
