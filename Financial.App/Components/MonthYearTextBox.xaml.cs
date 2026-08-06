using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.Components;

/// <summary>
/// Single-field Month + Year entry, formatted using the OS locale's short-date pattern with the
/// day component stripped (e.g. "MM/yyyy" for en-GB). Exposes the same SelectedYear/SelectedMonth
/// bindable contract as MonthYearPicker, so it can be swapped in wherever a compact single-field
/// control is preferred over the two-ComboBox layout.
/// </summary>
public partial class MonthYearTextBox : UserControl
{
    public static readonly DependencyProperty SelectedYearProperty = DependencyProperty.Register(
        nameof(SelectedYear), typeof(int), typeof(MonthYearTextBox),
        new PropertyMetadata(DateTime.Today.Year, OnSelectedPeriodChanged));

    public static readonly DependencyProperty SelectedMonthProperty = DependencyProperty.Register(
        nameof(SelectedMonth), typeof(int), typeof(MonthYearTextBox),
        new PropertyMetadata(DateTime.Today.Month, OnSelectedPeriodChanged));

    public int SelectedYear
    {
        get => (int)GetValue(SelectedYearProperty);
        set => SetValue(SelectedYearProperty, value);
    }

    public int SelectedMonth
    {
        get => (int)GetValue(SelectedMonthProperty);
        set => SetValue(SelectedMonthProperty, value);
    }

    public MonthYearTextBox()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncTextFromProperties();
    }

    private static void OnSelectedPeriodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MonthYearTextBox)d).SyncTextFromProperties();
    }

    private void SyncTextFromProperties()
    {
        var formatted = DateFormatHelper.FormatMonthYear(SelectedYear, SelectedMonth);
        if (textBox.Text != formatted)
        {
            textBox.Text = formatted;
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (TryParseMonthYear(textBox.Text, out var year, out var month))
        {
            SelectedYear = year;
            SelectedMonth = month;
        }

        SyncTextFromProperties();
    }

    private static bool TryParseMonthYear(string text, out int year, out int month)
    {
        var culture = CultureInfo.CurrentCulture;
        if (DateTime.TryParseExact(text, DateFormatHelper.GetMonthYearPattern(), culture, DateTimeStyles.None, out var exact)
            || DateTime.TryParse(text, culture, DateTimeStyles.None, out exact))
        {
            year = exact.Year;
            month = exact.Month;
            return true;
        }

        year = 0;
        month = 0;
        return false;
    }
}
