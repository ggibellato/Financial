using FinancialToolSupport;
using GoogleFinancialSupport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;

namespace ImportGoogleSpreadSheets;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public class FilesInfo : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public ObservableCollection<FilesInfo> Files { get; set; } = new ObservableCollection<FilesInfo>();

    private GoogleService _service;
    private const string AppSettingsFileName = "appsettings.json";
    private const string CredentialsPathConfigurationKey = "GoogleDrive:CredentialsPath";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        // Validate credentials file exists
        var credentialsPath = GetCredentialsPathFromConfig();
        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            MessageBox.Show($"Google API credentials file not found!\n\n" +
                           $"Configure '{CredentialsPathConfigurationKey}' in {AppSettingsFileName}.\n\n" +
                           $"Resolved location:\n{credentialsPath ?? "(not set)"}\n\n" +
                           "Please ensure the credentials file exists before using this application.",
                           "Credentials File Missing",
                           MessageBoxButton.OK,
                           MessageBoxImage.Error);
            btnConnect.IsEnabled = false;
            btnGenerateData.IsEnabled = false;
            UpdateStatus("Credentials file not found", "Configure credentials to continue");
            return;
        }
        
        try
        {
            _service = new GoogleService(credentialsPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing Google service:\n{ex.Message}",
                           "Initialization Error",
                           MessageBoxButton.OK,
                           MessageBoxImage.Error);
            btnConnect.IsEnabled = false;
            btnGenerateData.IsEnabled = false;
            UpdateStatus("Service initialization failed", "Check credentials file");
        }
    }

    private async void btnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_service == null)
        {
            MessageBox.Show("Service is not initialized. Please restart the application.",
                           "Service Error",
                           MessageBoxButton.OK,
                           MessageBoxImage.Error);
            return;
        }

        try
        {
            SetUIBusy(true, "Connecting to Google Drive...");
            
            var files = await _service.GetFilesNameAsync();
            Files.Clear();
            files.ForEach(f => { Files.Add(new FilesInfo { Id = f.Id, Name = f.Name, IsSelected = false}); });
            
            UpdateStatus($"Connected! Found {files.Count} files.", "Ready to generate data");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error connecting to Google Drive:\n{ex.Message}", "Connection Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Connection failed", "Error - check credentials");
        }
        finally
        {
            SetUIBusy(false);
        }
    }

    private async void btnGenerateData_Click(object sender, RoutedEventArgs e)
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        
        if (selectedFiles.Count == 0)
        {
            MessageBox.Show("Please select at least one file to import.", "No Files Selected", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetUIBusy(true, "Starting data generation...");
            
            IGenerator generator = new GoogleGenerator(_service, edtPath.Text);
            var fileNames = selectedFiles.Select(f => f.Name).ToList();

            var progress = new Progress<string>(status =>
            {
                UpdateStatus(status, $"Processing {fileNames.Count} file(s)...");
            });

            await generator.GenerateAsync(fileNames, progress);
            
            MessageBox.Show("Data generation completed successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Data generation complete!", "Ready for next operation");
        }
        catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("quota"))
        {
            MessageBox.Show($"Google API Rate Limit Exceeded:\n\n{ex.Message}\n\n" +
                           "Tips to avoid this:\n" +
                           "• Process fewer files at once\n" +
                           "• Wait a few minutes before retrying\n" +
                           "• The app now uses automatic retry with delays", 
                           "Rate Limit Error", 
                           MessageBoxButton.OK, MessageBoxImage.Warning);
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
            SetUIBusy(false);
        }
    }

    private void SetUIBusy(bool isBusy, string statusMessage = "")
    {
        btnConnect.IsEnabled = !isBusy;
        btnGenerateData.IsEnabled = !isBusy;
        cblFiles.IsEnabled = !isBusy;
        edtPath.IsEnabled = !isBusy;
        
        if (isBusy)
        {
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;
            if (!string.IsNullOrEmpty(statusMessage))
                txtStatus.Text = statusMessage;
        }
        else
        {
            progressBar.Visibility = Visibility.Collapsed;
            progressBar.IsIndeterminate = false;
        }
    }

    private void UpdateStatus(string mainStatus, string statusBarText)
    {
        txtStatus.Text = mainStatus;
        txtStatusBar.Text = statusBarText;
    }

    private static string? GetCredentialsPathFromConfig()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
        if (!File.Exists(settingsPath))
        {
            return null;
        }

        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream);
        if (!TryGetConfigValue(document.RootElement, CredentialsPathConfigurationKey, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var rawPath = value.GetString();
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        return Path.IsPathRooted(rawPath)
            ? rawPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rawPath));
    }

    private static bool TryGetConfigValue(JsonElement root, string key, out JsonElement value)
    {
        value = root;
        foreach (var segment in key.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                return false;
            }
        }
        return true;
    }
}
