using System.Text.Json.Serialization;
using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public sealed record SalesExceedPurchasesFinding(
    string BrokerName, string PortfolioName, string AssetName,
    DateTime OffendingSaleDate, decimal QuantityHeld, decimal Shortfall);

public sealed record UnpricedOpenHoldingFinding(string BrokerName, string PortfolioName, string AssetName);

public sealed record OpenHoldingMissingCostBasisFinding(string BrokerName, string PortfolioName, string AssetName);

public sealed record UnresolvedTaxClassificationFinding(
    string BrokerName, string PortfolioName, string AssetName, string TaxYear,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] EventCategory EventCategory);

public sealed record UnclassifiedHoldingFinding(
    string BrokerName, string PortfolioName, string AssetName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] InvestmentScope Scope);

public sealed record HistoricHoldingStillOpenFinding(
    string BrokerName, string PortfolioName, string AssetName, decimal Quantity, decimal CostOfUnitsHeld);

public sealed record CorporateActionAwaitingTaxReviewFinding(
    string BrokerName, string PortfolioName, string AssetName, Guid CorporateActionId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CorporateAction.CorporateActionType Type,
    DateTime EffectiveDate, string TaxYear);

public sealed class DataQualityReportDTO
{
    public IReadOnlyList<SalesExceedPurchasesFinding> SalesExceedPurchases { get; init; } = [];
    public IReadOnlyList<UnpricedOpenHoldingFinding> UnpricedOpenHoldings { get; init; } = [];
    public IReadOnlyList<OpenHoldingMissingCostBasisFinding> OpenHoldingsMissingCostBasis { get; init; } = [];
    public int StaleValuationCount { get; init; }
    public IReadOnlyList<UnresolvedTaxClassificationFinding> UnresolvedTaxClassifications { get; init; } = [];
    public IReadOnlyList<UnclassifiedHoldingFinding> UnclassifiedHoldings { get; init; } = [];
    public IReadOnlyList<HistoricHoldingStillOpenFinding> HistoricHoldingsStillOpen { get; init; } = [];
    public IReadOnlyList<UnclassifiedHoldingFinding> UnclassifiedAndUnpricedOpenHoldings { get; init; } = [];
    public IReadOnlyList<CorporateActionAwaitingTaxReviewFinding> CorporateActionsAwaitingTaxReview { get; init; } = [];
}
