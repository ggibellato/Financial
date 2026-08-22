using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Components;

public partial class MonthYearPicker : UserControl
{
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

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
        monthComboBox.ItemsSource = MonthNames;
        Loaded += (_, _) => SyncControlsFromProperties();
    }

    private static void OnSelectedPeriodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MonthYearPicker)d).SyncControlsFromProperties();
    }

    private void SyncControlsFromProperties()
    {
        RebuildYearItems();

        if (monthComboBox.SelectedIndex != SelectedMonth - 1)
        {
            monthComboBox.SelectedIndex = SelectedMonth - 1;
        }

        if (!Equals(yearComboBox.SelectedItem, SelectedYear))
        {
            yearComboBox.SelectedItem = SelectedYear;
        }
    }

    private void RebuildYearItems()
    {
        var years = Enumerable.Range(SelectedYear - 5, 7).ToList();
        if (yearComboBox.ItemsSource is not List<int> existing || !existing.SequenceEqual(years))
        {
            yearComboBox.ItemsSource = years;
        }
    }

    private void OnMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (monthComboBox.SelectedIndex < 0)
        {
            return;
        }

        var newMonth = monthComboBox.SelectedIndex + 1;
        if (newMonth != SelectedMonth)
        {
            SelectedMonth = newMonth;
        }
    }

    private void OnYearSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (yearComboBox.SelectedItem is int newYear && newYear != SelectedYear)
        {
            SelectedYear = newYear;
        }
    }
}
