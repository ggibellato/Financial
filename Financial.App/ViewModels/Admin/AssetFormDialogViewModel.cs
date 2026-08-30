using System.Text.RegularExpressions;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects an Asset's parent Broker/Portfolio (create only, fixed on edit) and identity fields,
/// mirroring <see cref="PortfolioFormDialogViewModel"/>'s shape: validates shape only, and lets the
/// domain's refusal (e.g. a duplicate name) surface as a save error on the owning list ViewModel
/// rather than being re-decided here.
/// </summary>
public sealed partial class AssetFormDialogViewModel : ViewModelBase
{
    private string _name;
    private string _isin = string.Empty;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    /// <summary>The Broker/Portfolio pickers' enabled state — bindable directly to IsEnabled, no converter needed.</summary>
    public bool CanChangeBrokerPortfolio => !IsEditing;

    public string Title => IsEditing ? "Edit Asset" : "Create Asset";

    private string _brokerName;

    public string BrokerName
    {
        get => _brokerName;
        set
        {
            if (SetProperty(ref _brokerName, value))
            {
                RefreshPortfoliosForBroker();
                Validate();
            }
        }
    }

    private string _portfolioName;

    public string PortfolioName
    {
        get => _portfolioName;
        set
        {
            if (SetProperty(ref _portfolioName, value))
            {
                Validate();
            }
        }
    }

    public IReadOnlyList<string> ActiveBrokerNames { get; }

    /// <summary>Every Active broker's portfolio names, keyed by broker, so changing the broker
    /// re-scopes the portfolio picker without a round-trip.</summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _portfolioNamesByBroker;

    private IReadOnlyList<string> _portfolioNames = [];

    public IReadOnlyList<string> PortfolioNames
    {
        get => _portfolioNames;
        private set => SetProperty(ref _portfolioNames, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                Validate();
            }
        }
    }

    public string ISIN
    {
        get => _isin;
        set
        {
            if (SetProperty(ref _isin, value))
            {
                Validate();
            }
        }
    }

    public string Exchange { get; set; } = string.Empty;

    public string Ticker { get; set; } = string.Empty;

    public string LocalTypeCode { get; set; } = string.Empty;

    public IReadOnlyList<CountryCode> CountryOptions { get; } = Enum.GetValues<CountryCode>();

    public CountryCode Country { get; set; } = CountryCode.Unknown;

    public IReadOnlyList<GlobalAssetClass> ClassOptions { get; } = Enum.GetValues<GlobalAssetClass>();

    /// <summary>Left at Unknown on create means "auto-resolve from Country/LocalTypeCode"; any other
    /// selection, or editing, is an explicit value. Mirrors Financial.Web's AssetFormDialog.</summary>
    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    /// <param name="portfolioNamesByBroker">Every Active broker's portfolio names, for the create-mode cascading picker. Ignored when editing.</param>
    /// <param name="existing">Set only when editing; fixes Broker/Portfolio and pre-fills identity fields.</param>
    public AssetFormDialogViewModel(IReadOnlyDictionary<string, IReadOnlyList<string>> portfolioNamesByBroker, AssetAdminDTO? existing = null)
    {
        IsEditing = existing is not null;
        _portfolioNamesByBroker = portfolioNamesByBroker;
        // A disabled ComboBox still needs its SelectedValue present in ItemsSource to display it,
        // the same reasoning PortfolioFormDialogViewModel uses for its Broker picker.
        ActiveBrokerNames = IsEditing ? [existing!.BrokerName] : portfolioNamesByBroker.Keys.ToList();
        _brokerName = existing?.BrokerName ?? ActiveBrokerNames.FirstOrDefault() ?? string.Empty;
        _portfolioName = existing?.PortfolioName ?? string.Empty;
        _name = existing?.Name ?? string.Empty;
        _isin = existing?.ISIN ?? string.Empty;
        Exchange = existing?.Exchange ?? string.Empty;
        Ticker = existing?.Ticker ?? string.Empty;
        LocalTypeCode = existing?.LocalTypeCode ?? string.Empty;
        Country = existing?.Country ?? CountryCode.Unknown;
        Class = existing?.Class ?? GlobalAssetClass.Unknown;

        PortfolioNames = IsEditing ? [existing!.PortfolioName] : (portfolioNamesByBroker.GetValueOrDefault(_brokerName) ?? []);
        _portfolioName = IsEditing ? _portfolioName : (PortfolioNames.FirstOrDefault() ?? string.Empty);

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    private void RefreshPortfoliosForBroker()
    {
        if (IsEditing)
        {
            return;
        }

        PortfolioNames = _portfolioNamesByBroker.GetValueOrDefault(BrokerName) ?? [];
        PortfolioName = PortfolioNames.Contains(PortfolioName) ? PortfolioName : (PortfolioNames.FirstOrDefault() ?? string.Empty);
    }

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        Name = Name.Trim();
        ISIN = ISIN.Trim();
        CloseRequested?.Invoke(this, true);
    }

    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        ValidationMessage = string.IsNullOrWhiteSpace(Name)
            ? "Name is required."
            : !IsEditing && string.IsNullOrWhiteSpace(BrokerName)
                ? "A broker is required."
                : !IsEditing && string.IsNullOrWhiteSpace(PortfolioName)
                    ? "A portfolio is required."
                    : !string.IsNullOrWhiteSpace(ISIN) && !IsinPattern().IsMatch(ISIN.Trim())
                        ? "ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005)."
                        : string.Empty;
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    private static partial Regex IsinPattern();
}
