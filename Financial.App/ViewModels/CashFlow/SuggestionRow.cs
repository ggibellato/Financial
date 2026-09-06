using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public enum SuggestionRowStatus
{
    Pending,
    Success,
    Error
}

/// <summary>A single row of the Suggest Values panel: the account's current and suggested
/// snapshot value, its computed source description, and the user's editable Include/Value
/// choices. <see cref="SnapshotId"/> is what Apply writes to via the existing snapshot update
/// endpoint - this row never talks to a persisted entity of its own.</summary>
public sealed class SuggestionRow : ViewModelBase
{
    private bool _included;
    private string _suggestedValue;
    private SuggestionRowStatus _status = SuggestionRowStatus.Pending;

    public Guid SnapshotId { get; }
    public Guid AccountId { get; }
    public string AccountName { get; }
    public decimal CurrentValue { get; }
    public string SourceDescription { get; }

    public string SuggestedValue
    {
        get => _suggestedValue;
        set => SetProperty(ref _suggestedValue, value);
    }

    public bool Included
    {
        get => _included;
        set
        {
            if (SetProperty(ref _included, value))
            {
                OnPropertyChanged(nameof(IsOverwriteCandidate));
            }
        }
    }

    public SuggestionRowStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>True when a checked row would overwrite a non-zero existing value - drives the
    /// muted/struck-through current-value display.</summary>
    public bool IsOverwriteCandidate => Included && CurrentValue != 0;

    public SuggestionRow(InvestmentSnapshotSuggestionDTO dto)
    {
        SnapshotId = dto.SnapshotId;
        AccountId = dto.AccountId;
        AccountName = dto.AccountName;
        CurrentValue = dto.CurrentValue;
        SourceDescription = dto.SourceDescription;
        _suggestedValue = dto.SuggestedValue.ToString();
        _included = dto.CurrentValue == 0;
    }
}

public sealed class SuggestionSkippedRow(Guid accountId, string accountName, string reason)
{
    public Guid AccountId { get; } = accountId;
    public string AccountName { get; } = accountName;
    public string Reason { get; } = reason;

    public static SuggestionSkippedRow FromDto(InvestmentSnapshotSuggestionSkippedDTO dto) =>
        new(dto.AccountId, dto.AccountName, dto.Reason);
}
