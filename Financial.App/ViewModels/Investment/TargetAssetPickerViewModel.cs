using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class TargetAssetPickerViewModel : ViewModelBase
{
    private readonly IReadOnlyList<AssetAdminDTO> _candidates;
    private string _assetName = string.Empty;
    private AssetAdminDTO? _matchedAsset;
    private string _isin = string.Empty;
    private string? _nameError;

    public IReadOnlyList<string> CandidateNames { get; }

    public string AssetName
    {
        get => _assetName;
        set
        {
            if (SetProperty(ref _assetName, value))
            {
                _matchedAsset = FindMatch(value);
                OnPropertyChanged(nameof(TrimmedAssetName));
                OnPropertyChanged(nameof(MatchedAsset));
                OnPropertyChanged(nameof(ShowCreateFields));
                NameError = null;
            }
        }
    }

    public string TrimmedAssetName => AssetName.Trim();

    public AssetAdminDTO? MatchedAsset => _matchedAsset;

    public bool ShowCreateFields => TrimmedAssetName.Length > 0 && MatchedAsset is null;

    public string ISIN
    {
        get => _isin;
        set
        {
            if (SetProperty(ref _isin, value))
            {
                OnPropertyChanged(nameof(IsinValidationMessage));
            }
        }
    }

    public string Exchange { get; set; } = string.Empty;

    public string Ticker { get; set; } = string.Empty;

    public IReadOnlyList<CountryCode> CountryOptions => AssetIdentityOptions.CountryOptions;

    public CountryCode Country { get; set; } = CountryCode.Unknown;

    public IReadOnlyList<GlobalAssetClass> ClassOptions => AssetIdentityOptions.ClassOptions;

    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;

    public string? IsinValidationMessage => AssetIdentityValidation.ValidateIsin(ISIN);

    public string? NameError
    {
        get => _nameError;
        set => SetProperty(ref _nameError, value);
    }

    public TargetAssetPickerViewModel(IReadOnlyList<AssetAdminDTO> candidates)
    {
        _candidates = candidates;
        CandidateNames = candidates
            .Select(a => a.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AssetAdminDTO? FindMatch(string name)
    {
        var trimmed = name.Trim();
        return _candidates.FirstOrDefault(a => string.Equals(a.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
