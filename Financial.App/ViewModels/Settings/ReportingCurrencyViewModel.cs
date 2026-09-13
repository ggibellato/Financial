using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;
using Microsoft.Extensions.Logging;

namespace Financial.Presentation.App.ViewModels.Settings;

/// <summary>
/// Reads the current setting synchronously and in-process (<see cref="IReportingCurrencyProvider.GetReportingCurrency"/>
/// is a plain in-memory read, not an HTTP round trip like F04's React page), so unlike
/// <see cref="SettingsIntegrationsViewModel"/> there is no loading state to model - only the save
/// itself is asynchronous, and only a save failure is left visible until the next selection.
/// </summary>
public class ReportingCurrencyViewModel : ViewModelBase
{
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private readonly ILogger<ReportingCurrencyViewModel> _logger;
    private Currency _currency;
    private bool _isEnabled;
    private string? _saveError;

    public ReportingCurrencyViewModel(IReportingCurrencyProvider reportingCurrencyProvider, ILogger<ReportingCurrencyViewModel> logger)
    {
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currency = _reportingCurrencyProvider.GetReportingCurrency();
        _isEnabled = _reportingCurrencyProvider.IsReportingCurrencyEnabled();
    }

    public string? SaveError { get => _saveError; private set => SetProperty(ref _saveError, value); }

    /// <summary>Also gates the currency radio buttons' XAML <c>IsEnabled</c> - choosing a currency
    /// has no effect while conversion itself is off.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (value != _isEnabled) _ = SetEnabledAsync(value); }
    }

    public bool IsGbpSelected
    {
        get => _currency == Currency.GBP;
        set { if (value) _ = SetCurrencyAsync(Currency.GBP); }
    }

    public bool IsBrlSelected
    {
        get => _currency == Currency.BRL;
        set { if (value) _ = SetCurrencyAsync(Currency.BRL); }
    }

    public bool IsUsdSelected
    {
        get => _currency == Currency.USD;
        set { if (value) _ = SetCurrencyAsync(Currency.USD); }
    }

    internal async Task SetCurrencyAsync(Currency currency)
    {
        if (currency == _currency)
        {
            return;
        }

        SaveError = null;

        try
        {
            await _reportingCurrencyProvider.SetReportingCurrencyAsync(currency);
            _currency = currency;
        }
        catch (Exception ex)
        {
            _logger.LogError("ReportingCurrency save failed with {ErrorType}", ex.GetType().Name);
            SaveError = ex.Message;
        }
        finally
        {
            NotifySelectionChanged();
        }
    }

    internal async Task SetEnabledAsync(bool enabled)
    {
        SaveError = null;

        try
        {
            await _reportingCurrencyProvider.SetReportingCurrencyEnabledAsync(enabled);
            _isEnabled = enabled;
        }
        catch (Exception ex)
        {
            _logger.LogError("ReportingCurrency enabled save failed with {ErrorType}", ex.GetType().Name);
            SaveError = ex.Message;
        }
        finally
        {
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(IsGbpSelected));
        OnPropertyChanged(nameof(IsBrlSelected));
        OnPropertyChanged(nameof(IsUsdSelected));
    }
}
