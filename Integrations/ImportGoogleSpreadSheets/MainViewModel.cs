using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Financial.Investment.Infrastructure.Integrations.ImportGoogleSpreadSheets;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly string DefaultOutputPath = Path.Combine(AppContext.BaseDirectory, "data");

    private readonly GoogleService? _service;
    private readonly IInvestmentsSerializer _serializer;
    private readonly RelayCommand _connectCommand;
    private readonly RelayCommand _generateCommand;
    private bool _isBusy;
    private string _status = "Ready";
    private string _statusBar = "Ready to import";
    private string _outputPath = DefaultOutputPath;

    public ObservableCollection<FilesInfo> Files { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
            _connectCommand.RaiseCanExecuteChanged();
            _generateCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsNotBusy => !_isBusy;

    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(nameof(Status)); }
    }

    public string StatusBar
    {
        get => _statusBar;
        private set { _statusBar = value; OnPropertyChanged(nameof(StatusBar)); }
    }

    public string OutputPath
    {
        get => _outputPath;
        set { _outputPath = value; OnPropertyChanged(nameof(OutputPath)); }
    }

    public ICommand ConnectCommand { get; }
    public ICommand GenerateCommand { get; }

    public MainViewModel(GoogleService? service, IInvestmentsSerializer serializer)
    {
        _service = service;
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _connectCommand = new RelayCommand(async () => await ConnectAsync(), () => _service != null && !IsBusy);
        _generateCommand = new RelayCommand(async () => await GenerateAsync(), () => _service != null && !IsBusy);
        ConnectCommand = _connectCommand;
        GenerateCommand = _generateCommand;
    }

    private async Task ConnectAsync()
    {
        if (_service == null) return;
        try
        {
            IsBusy = true;
            Status = "Connecting to Google Drive...";

            var files = await _service.GetFilesNameAsync();
            var filtered = files
                .Where(f => !string.Equals(f.Name, "data.json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Files.Clear();
            filtered.ForEach(f => Files.Add(new FilesInfo { Id = f.Id, Name = f.Name }));

            UpdateStatus($"Connected! Found {filtered.Count} files.", "Ready to generate data");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error connecting to Google Drive:\n{ex.Message}", "Connection Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Connection failed", "Error - check credentials");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateAsync()
    {
        if (_service == null) return;

        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (selectedFiles.Count == 0)
        {
            MessageBox.Show("Please select at least one file to import.", "No Files Selected",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            Status = "Starting data generation...";

            var generator = new GoogleGenerator(
                _service,
                new LocalJsonStorage(OutputPath),
                GoogleGeneratorConfiguration.BuildOptions(),
                _serializer);

            var fileNames = selectedFiles.Select(f => f.Name).ToList();
            var progress = new Progress<string>(msg => UpdateStatus(msg, $"Processing {fileNames.Count} file(s)..."));

            await generator.GenerateAsync(fileNames, progress);

            MessageBox.Show("Data generation completed successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Data generation complete!", "Ready for next operation");
        }
        catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("quota"))
        {
            MessageBox.Show(
                $"Google API Rate Limit Exceeded:\n\n{ex.Message}\n\n" +
                "Tips to avoid this:\n" +
                "• Process fewer files at once\n" +
                "• Wait a few minutes before retrying\n" +
                "• The app now uses automatic retry with delays",
                "Rate Limit Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateStatus("Rate limit exceeded", "Wait before retrying");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating data:\n{ex.Message}", "Generation Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Generation failed", "Error occurred");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStatus(string status, string statusBar)
    {
        Status = status;
        StatusBar = statusBar;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
