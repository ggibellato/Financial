using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Financial.CashFlow.Infrastructure.Persistence;

public class CashFlowTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    private static readonly HashSet<Type> ManagedTypes =
    [
        typeof(CashFlowData),
        typeof(Expense),
        typeof(ReserveMovement),
        typeof(CardStatement),
        typeof(RecurringBill),
        typeof(MaeLedgerEntry),
        typeof(InvestmentSnapshot),
        typeof(InvestmentAccount),
        typeof(Bank),
        typeof(IncomeSource),
        typeof(ReserveBucket),
        typeof(CreditCard),
        typeof(Category),
        typeof(Income),
        typeof(Transfer),
        typeof(BalanceAdjustment),
        typeof(TitheCarryForward)
    ];

    // Maps each reference-typed property to its wire name and whether the key must be present in
    // JSON; the reference converter itself is resolved separately per-property since it needs the
    // resolver's own context instance. IsRequired is true for every property that was already
    // present (even if null-valued) in every pre-existing record the day it was introduced -
    // ReserveMovement.Income is the first exception: a brand-new key with zero prior occurrences,
    // so an absent key must be tolerated (it just means "not linked") rather than rejected.
    private static readonly Dictionary<(Type OwningType, string PropertyName), (string WireName, bool IsRequired)> ReferenceProperties = new()
    {
        [(typeof(Income), nameof(Income.Bank))] = ("BankId", true),
        [(typeof(Income), nameof(Income.IncomeSource))] = ("IncomeSourceId", true),
        [(typeof(Expense), nameof(Expense.PaymentSourceBank))] = ("PaymentSourceBankId", true),
        [(typeof(Transfer), nameof(Transfer.SourceBank))] = ("SourceBankId", true),
        [(typeof(Transfer), nameof(Transfer.DestinationBank))] = ("DestinationBankId", true),
        [(typeof(BalanceAdjustment), nameof(BalanceAdjustment.Bank))] = ("BankId", true),
        [(typeof(InvestmentSnapshot), nameof(InvestmentSnapshot.Account))] = ("InvestmentAccountId", true),
        [(typeof(ReserveMovement), nameof(ReserveMovement.Bucket))] = ("BucketId", true),
        [(typeof(Expense), nameof(Expense.CreditCard))] = ("CreditCardId", true),
        [(typeof(CardStatement), nameof(CardStatement.CreditCard))] = ("CreditCardId", true),
        [(typeof(Expense), nameof(Expense.Category))] = ("CategoryId", true),
        [(typeof(ReserveMovement), nameof(ReserveMovement.Income))] = ("IncomeId", false),
    };

    private readonly ReferenceResolutionContext? _context;

    public CashFlowTypeInfoResolver(ReferenceResolutionContext? context = null)
    {
        _context = context;
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (!ManagedTypes.Contains(type) || typeInfo.Kind != JsonTypeInfoKind.Object)
            return typeInfo;

        ReflectionJsonTypeInfoHelpers.EnablePrivateConstructor(type, typeInfo);
        ConfigureProperties(type, typeInfo);

        return typeInfo;
    }

    private void ConfigureProperties(Type type, JsonTypeInfo typeInfo)
    {
        foreach (var jsonProp in typeInfo.Properties)
        {
            ReflectionJsonTypeInfoHelpers.WirePropertySetter(type, jsonProp);
            ConfigureReferenceProperty(type, jsonProp);
        }
    }

    private void ConfigureReferenceProperty(Type type, JsonPropertyInfo jsonProp)
    {
        if (!ReferenceProperties.TryGetValue((type, jsonProp.Name), out var reference))
            return;

        jsonProp.Name = reference.WireName;
        jsonProp.IsRequired = reference.IsRequired;
        jsonProp.CustomConverter = CreateReferenceConverter(jsonProp.PropertyType);
    }

    private JsonConverter CreateReferenceConverter(Type propertyType)
    {
        if (propertyType == typeof(Bank))
            return new BankReferenceConverter(_context?.Banks);

        if (propertyType == typeof(IncomeSource))
            return new IncomeSourceReferenceConverter(_context?.IncomeSources);

        if (propertyType == typeof(InvestmentAccount))
            return new InvestmentAccountReferenceConverter(_context?.InvestmentAccounts);

        if (propertyType == typeof(ReserveBucket))
            return new ReserveBucketReferenceConverter(_context?.ReserveBuckets);

        if (propertyType == typeof(CreditCard))
            return new CreditCardReferenceConverter(_context?.CreditCards);

        if (propertyType == typeof(Category))
            return new CategoryReferenceConverter(_context?.Categories);

        if (propertyType == typeof(Income))
            return new IncomeReferenceConverter(_context?.Incomes);

        throw new InvalidOperationException($"No reference converter registered for type {propertyType}.");
    }
}
