using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Collects where an asset should be moved to: an existing portfolio of the same broker, or a new
/// one named here and created by the move.
/// </summary>
/// <remarks>
/// Validates shape only - that a destination was chosen, or that a typed name is not blank. Whether
/// the destination is legal is the domain's to decide, and its refusal is what the user is shown.
/// Re-deciding it here would put the same rule in two places and let the wordings drift apart.
/// </remarks>
public sealed class MoveAssetDialogViewModel : ViewModelBase
{
    private string? _selectedPortfolioName;
    private string _newPortfolioName = string.Empty;
    private bool _createNewPortfolio;
    private string _validationMessage = string.Empty;

    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    /// <summary>Destinations offered: the broker's other portfolios. The one the asset is already
    /// in is left out rather than shown and then refused.</summary>
    public ObservableCollection<string> AvailablePortfolios { get; }

    /// <summary>False when the broker has no other portfolio, so naming a new one is the only route.</summary>
    public bool HasExistingDestination => AvailablePortfolios.Count > 0;

    public string Title => "Move Asset";

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
        IEnumerable<string> existingPortfolioNames)
    {
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;

        AvailablePortfolios = new ObservableCollection<string>(
            (existingPortfolioNames ?? []).Where(name => name != portfolioName));

        // Nowhere to move to yet means the only way forward is to name a portfolio, so start there
        // rather than on an empty list the user cannot act on.
        _createNewPortfolio = AvailablePortfolios.Count == 0;
        _selectedPortfolioName = AvailablePortfolios.FirstOrDefault();

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

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
