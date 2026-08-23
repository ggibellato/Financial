using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Financial.Presentation.App.Components;

public partial class MonthYearPicker : UserControl
{
    private readonly Button[] _monthButtons = new Button[12];
    private int _displayYear;

    public static readonly DependencyProperty SelectedYearProperty = DependencyProperty.Register(
        nameof(SelectedYear), typeof(int), typeof(MonthYearPicker),
        new PropertyMetadata(DateTime.Today.Year, OnSelectedPeriodChanged));

    public static readonly DependencyProperty SelectedMonthProperty = DependencyProperty.Register(
        nameof(SelectedMonth), typeof(int), typeof(MonthYearPicker),
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

    public MonthYearPicker()
    {
        InitializeComponent();
        BuildMonthButtons();
        Loaded += (_, _) => UpdateTriggerText();
    }

    private void BuildMonthButtons()
    {
        var flatStyle = (Style)FindResource("FlatMonthButtonStyle");
        for (var month = 1; month <= 12; month++)
        {
            var button = new Button
            {
                Content = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                Tag = month,
                Style = flatStyle,
            };
            AutomationProperties.SetName(button, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month));
            button.Click += OnMonthButtonClick;
            _monthButtons[month - 1] = button;
            monthsGrid.Children.Add(button);
        }
    }

    private static void OnSelectedPeriodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MonthYearPicker)d).UpdateTriggerText();
    }

    private void UpdateTriggerText()
    {
        triggerButton.Content = new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");
    }

    private void OnTriggerButtonClick(object sender, RoutedEventArgs e)
    {
        _displayYear = SelectedYear;
        RefreshPopupContent();
        popup.IsOpen = true;
    }

    private void OnPreviousYearClick(object sender, RoutedEventArgs e)
    {
        _displayYear--;
        RefreshPopupContent();
    }

    private void OnNextYearClick(object sender, RoutedEventArgs e)
    {
        _displayYear++;
        RefreshPopupContent();
    }

    private void RefreshPopupContent()
    {
        var flatStyle = (Style)FindResource("FlatMonthButtonStyle");
        var selectedStyle = (Style)FindResource("SelectedMonthButtonStyle");
        yearHeaderText.Text = _displayYear.ToString(CultureInfo.CurrentCulture);
        for (var i = 0; i < _monthButtons.Length; i++)
        {
            var isSelected = _displayYear == SelectedYear && i + 1 == SelectedMonth;
            _monthButtons[i].Style = isSelected ? selectedStyle : flatStyle;
        }
    }

    private void OnMonthButtonClick(object sender, RoutedEventArgs e)
    {
        var month = (int)((Button)sender).Tag;
        SelectedYear = _displayYear;
        SelectedMonth = month;
        popup.IsOpen = false;
    }
}
