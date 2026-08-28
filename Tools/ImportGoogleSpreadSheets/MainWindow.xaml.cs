using Financial.Integrations.GoogleDrive;
using Financial.Integrations.GoogleSheets;
using Financial.Investment.Infrastructure.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Financial.Investment.Infrastructure.Tools.ImportGoogleSpreadSheets;

public partial class MainWindow : Window
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string CredentialsPathConfigurationKey = "GoogleDrive:CredentialsPath";

    public MainWindow()
    {
        InitializeComponent();

        var credentialsPath = ResolveCredentialsPath();
        var (driveFiles, sheets) = TryCreateClients(credentialsPath);
        DataContext = new MainViewModel(driveFiles, sheets, new InvestmentSerializerAdapter());
    }

    private (IGoogleDriveFileSource?, IGoogleSheetsDataSource?) TryCreateClients(string? credentialsPath)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            MessageBox.Show(
                $"Google API credentials file not found!\n\n" +
                $"Configure '{CredentialsPathConfigurationKey}' in {AppSettingsFileName}.\n\n" +
                $"Resolved location:\n{credentialsPath ?? "(not set)"}\n\n" +
                "Please ensure the credentials file exists before using this application.",
                "Credentials File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            return (null, null);
        }

        try
        {
            return (new GoogleDriveFileClient(credentialsPath), new GoogleSheetsDataSource(credentialsPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing Google service:\n{ex.Message}",
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return (null, null);
        }
    }

    private static string? ResolveCredentialsPath()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
        if (!File.Exists(settingsPath))
            return null;

        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream);
        if (!TryGetConfigValue(document.RootElement, CredentialsPathConfigurationKey, out var value)
            || value.ValueKind != JsonValueKind.String)
            return null;

        var rawPath = value.GetString();
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

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
                return false;
        }
        return true;
    }
}
