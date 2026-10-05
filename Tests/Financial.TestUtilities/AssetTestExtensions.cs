using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class AssetTestExtensions
{
    public static void RecordTransaction(
        this Asset asset,
        Transaction transaction,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        IReadOnlyList<SpecificLotAllocation>? allocation = null,
        Investments? investments = null) =>
        asset.RecordTransaction(transaction, TestClock.Default, method, allocation, investments);

    public static bool ReviseTransaction(
        this Asset asset,
        Transaction transaction,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        Investments? investments = null) =>
        asset.ReviseTransaction(transaction, TestClock.Default, method, investments);

    public static bool RetractTransaction(
        this Asset asset,
        Guid transactionId,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        Investments? investments = null) =>
        asset.RetractTransaction(transactionId, TestClock.Default, method, investments);

    public static void RecordCorporateAction(
        this Asset asset,
        CorporateAction corporateAction,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        Investments? investments = null,
        Currency? brokerCurrency = null) =>
        asset.RecordCorporateAction(corporateAction, TestClock.Default, method, investments, brokerCurrency);

    public static bool ReviseCorporateAction(
        this Asset asset,
        CorporateAction corporateAction,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        Investments? investments = null,
        Currency? brokerCurrency = null) =>
        asset.ReviseCorporateAction(corporateAction, TestClock.Default, method, investments, brokerCurrency);

    public static bool RetractCorporateAction(
        this Asset asset,
        Guid corporateActionId,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        Investments? investments = null) =>
        asset.RetractCorporateAction(corporateActionId, TestClock.Default, method, investments);

    public static void AddCredit(this Asset asset, Credit credit, Investments? investments = null) =>
        asset.AddCredit(credit, TestClock.Default, investments);

    public static bool UpdateCredit(this Asset asset, Credit credit, Investments? investments = null) =>
        asset.UpdateCredit(credit, TestClock.Default, investments);

    // today = date: fixtures never trip the future-date rule; call the 7-argument SetPrice to test it.
    public static void SetPrice(this Asset asset, DateOnly date, decimal price, bool isManual) =>
        asset.SetPrice(date, price, isManual ? PriceSource.Manual : PriceSource.Unknown, string.Empty, null, TestClock.Default, date);
}
