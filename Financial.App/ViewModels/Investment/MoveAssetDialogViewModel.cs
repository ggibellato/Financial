using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Collects where an asset should go: an existing portfolio of the same broker, or a new one named
/// here. When the asset is a closed position in Active Investments it can also be sent to Historic
/// Investments, and the same existing-or-new choice then applies to the Historic portfolios.
/// </summary>
/// <remarks>
/// Validates shape only - that a destination was chosen, or that a typed name is not blank. Whether
/// the destination is legal is the domain's to decide, and its refusal is what the user is shown.
/// Re-deciding it here would put the same rule in two places and let the wordings drift apart.
/// </remarks>
public sealed class MoveAssetDialogViewModel : ViewModelBase
{
    private readonly string[] _samePortfolioNames;
    private readonly string[] _historicPortfolioNames;
    private string? _selectedPortfolioName;
    private string _newPortfolioName = string.Empty;
    private bool _createNewPortfolio;
    private bool _archiveToHistoric;
    private string _validationMessage = string.Empty;

    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    /// <summary>
    /// Whether Historic Investments is offered as a destination at all. False for an asset that
    /// still holds a position, and false for one already in Historic - an asset never comes back
    /// out of the archive.
    /// </summary>
    public bool CanArchive { get; }

    /// <summary>Destinations for the scope currently chosen.</summary>
    public ObservableCollection<string> AvailablePortfolios { get; } = [];

    /// <summary>False when the chosen scope offers nothing, so naming a new portfolio is the only route.</summary>
    public bool HasExistingDestination => AvailablePortfolios.Count > 0;

    public string Title => "Move Asset";

    /// <summary>True when the destination is a Historic portfolio, so the caller archives instead of moving.</summary>
    public bool ArchiveToHistoric
    {
        get => _archiveToHistoric;
        set
        {
            if (SetProperty(ref _archiveToHistoric, value))
            {
                OnPropertyChanged(nameof(KeepInCurrentScope));
                RefreshDestinations();
            }
        }
    }

    public bool KeepInCurrentScope
    {
        get => !ArchiveToHistoric;
        set => ArchiveToHistoric = !value;
    }

    public string? SelectedPortfolioName
    {
        get => _selectedPortfolioName;
        set
        {
            if (SetProperty(ref _selectedPortfolioName, value))
            {
                Validate();
            }
        }
    }

    public string NewPortfolioName
    {
        get => _newPortfolioName;
        set
        {
            if (SetProperty(ref _newPortfolioName, value))
            {
                Validate();
            }
        }
    }

    public bool CreateNewPortfolio
    {
        get => _createNewPortfolio;
        set
        {
            if (SetProperty(ref _createNewPortfolio, value))
            {
                OnPropertyChanged(nameof(UseExistingPortfolio));
                Validate();
            }
        }
    }

    public bool UseExistingPortfolio
    {
        get => !CreateNewPortfolio;
        set => CreateNewPortfolio = !value;
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>The chosen destination. Empty while nothing has been chosen or typed.</summary>
    public string DestinationPortfolioName =>
        CreateNewPortfolio ? NewPortfolioName.Trim() : SelectedPortfolioName ?? string.Empty;

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public MoveAssetDialogViewModel(
        string brokerName,
        string portfolioName,
        string assetName,
        IEnumerable<string> existingPortfolioNames,
        IEnumerable<string>? historicPortfolioNames = null,
        bool canArchive = false)
    {
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        CanArchive = canArchive;

        _samePortfolioNames = (existingPortfolioNames ?? []).Where(name => name != portfolioName).ToArray();

        // Every Historic portfolio is a candidate, including one named like the source: across
        // scopes that is a different portfolio, not the one the asset is already in.
        _historicPortfolioNames = (historicPortfolioNames ?? []).ToArray();

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        RefreshDestinations();
    }

    private void RefreshDestinations()
    {
        var destinations = ArchiveToHistoric ? _historicPortfolioNames : _samePortfolioNames;

        AvailablePortfolios.Clear();
        foreach (var name in destinations)
        {
            AvailablePortfolios.Add(name);
        }

        OnPropertyChanged(nameof(HasExistingDestination));

        // Nothing to choose from means the only way forward is to name a portfolio, so start there
        // rather than on an empty list the user cannot act on.
        SelectedPortfolioName = AvailablePortfolios.FirstOrDefault();
        CreateNewPortfolio = AvailablePortfolios.Count == 0;

        Validate();
    }

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        ValidationMessage = CreateNewPortfolio
            ? NewPortfolioName.Trim().Length == 0 ? "Enter a name for the new portfolio." : string.Empty
            : string.IsNullOrWhiteSpace(SelectedPortfolioName) ? "Select the portfolio to move the asset into." : string.Empty;

        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
