using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Infrastructure.Persistence;
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
        typeof(Income),
        typeof(Transfer),
        typeof(BalanceAdjustment)
    ];

    // Maps each reference-typed property to its wire name; the reference converter itself is
    // resolved separately per-property since it needs the resolver's own context instance.
    private static readonly Dictionary<(Type OwningType, string PropertyName), string> ReferenceProperties = new()
    {
        [(typeof(Income), nameof(Income.Bank))] = "BankId",
        [(typeof(Income), nameof(Income.IncomeSource))] = "IncomeSourceId",
        [(typeof(Expense), nameof(Expense.PaymentSourceBank))] = "PaymentSourceBankId",
        [(typeof(Transfer), nameof(Transfer.SourceBank))] = "SourceBankId",
        [(typeof(Transfer), nameof(Transfer.DestinationBank))] = "DestinationBankId",
        [(typeof(BalanceAdjustment), nameof(BalanceAdjustment.Bank))] = "BankId",
        [(typeof(InvestmentSnapshot), nameof(InvestmentSnapshot.Account))] = "InvestmentAccountId",
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
        if (!ReferenceProperties.TryGetValue((type, jsonProp.Name), out var wireName))
            return;

        jsonProp.Name = wireName;
        jsonProp.IsRequired = true;
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

        throw new InvalidOperationException($"No reference converter registered for type {propertyType}.");
    }
}
