using System.Windows;
using FinancialUI.ViewModels;

namespace FinancialUI;

public partial class MainWindow : Window
{
    private readonly MainNavigationViewModel _viewModel;

    public MainWindow(MainNavigationViewModel viewModel)
    {
        InitializeComponent();
        
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadNavigationTreeAsync();
    }
}
