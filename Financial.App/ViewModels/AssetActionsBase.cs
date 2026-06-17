using System;
using System.Windows;
using Financial.Application.DTOs;

namespace Financial.Presentation.App.ViewModels;

public abstract class AssetActionsBase
{
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;
    private readonly string _title;

    protected AssetActionsBase(
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage,
        string title)
    {
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        _title = title;
    }

    protected bool HasContext() => _hasContext();
    protected string GetBrokerName() => _brokerName();
    protected string GetPortfolioName() => _portfolioName();
    protected string GetAssetName() => _assetName();
    protected void ApplyDetails(AssetDetailsDTO details) => _applyDetails(details);

    protected void ShowInfo(string message) =>
        _showMessage(message, _title, MessageBoxImage.Information);

    protected void ShowWarning(string message) =>
        _showMessage(message, _title, MessageBoxImage.Warning);
}
