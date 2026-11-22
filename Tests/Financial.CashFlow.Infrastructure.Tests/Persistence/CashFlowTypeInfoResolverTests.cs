using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Financial.TestUtilities;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Unit")]
public class CashFlowTypeInfoResolverTests
{
    private static JsonSerializerOptions CreateOptions(ReferenceResolutionContext? context = null) => new()
    {
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };

    public static TheoryData<Type, string, bool> ReferenceProperties => new()
    {
        { typeof(Income), "AccountId", true },
        { typeof(Income), "IncomeSourceId", true },
        { typeof(Expense), "PaymentSourceAccountId", true },
        { typeof(Transfer), "SourceAccountId", true },
        { typeof(Transfer), "DestinationAccountId", true },
        { typeof(BalanceAdjustment), "AccountId", true },
        { typeof(SettlementTransaction), "AccountId", true },
        { typeof(InvestmentSnapshot), "InvestmentAccountId", true },
        { typeof(PotTransaction), "BucketId", true },
        { typeof(Expense), "CreditCardId", true },
        { typeof(CardStatement), "CreditCardId", true },
        { typeof(Expense), "CategoryId", true },
        { typeof(PotTransaction), "IncomeId", false },
        { typeof(InvestmentAccount), "CreditCardId", false },
        { typeof(InvestmentAccount), "PotsAccountId", false },
        { typeof(IncomeSource), "PotsAccountId", false },
        { typeof(Income), "PotsAccountId", false }
    };

    [Theory]
    [MemberData(nameof(ReferenceProperties))]
    public void GetTypeInfo_ReferenceProperty_IsWrittenUnderItsWireNameAndRequiredOnlyWhenEveryRecordHasIt(Type owningType, string wireName, bool isRequired)
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(owningType, options);

        typeInfo!.Properties.Should().ContainSingle(p => p.Name == wireName).Which.IsRequired.Should().Be(isRequired);
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_EnablesPrivateConstructor()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Expense), options);

        typeInfo!.CreateObject.Should().NotBeNull();
    }

    public static TheoryData<Type> ManagedTypes => new(CashFlowTypeInfoResolver.ManagedTypes);

    [Theory]
    [MemberData(nameof(ManagedTypes))]
    public void GetTypeInfo_ForManagedType_WritesExactlyThePropertiesWithASetter(Type type)
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(type, options);

        StoredJsonProperties.Written(typeInfo!).Should().BeEquivalentTo(StoredJsonProperties.Settable(type));
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_WiresSettableProperties()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Expense), options);

        var descriptionProp = typeInfo!.Properties.Should().ContainSingle(p => p.Name == nameof(Expense.Description)).Subject;
        descriptionProp.Set.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_ForUnmanagedType_ReturnsUnmodifiedTypeInfo()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(string), options);

        typeInfo.Should().NotBeNull();
        typeInfo!.Kind.Should().Be(JsonTypeInfoKind.None);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsExpenseThroughPrivateConstructor()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        var category = Category.Create("Mercado");
        var context = new ReferenceResolutionContext();
        context.Accounts[bank.Id] = bank;
        context.Categories[category.Id] = category;
        var options = CreateOptions(context);
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Weekly groceries", 54.32m, category, bank, null);

        var json = JsonSerializer.Serialize(expense, CreateOptions());
        var deserialized = JsonSerializer.Deserialize<Expense>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(expense.Id);
        deserialized.Description.Should().Be("Weekly groceries");
        deserialized.Value.Should().Be(54.32m);
        deserialized.Category.Should().BeSameAs(category);
        deserialized.PaymentSourceAccount.Should().BeSameAs(bank);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsExpense_RecomputesPaymentStatusFromCreditCardAndPaymentSource()
    {
        var creditCard = TestAccounts.CreditCard("Chase Mastercard 4023");
        var category = Category.Create("Extras");
        var context = new ReferenceResolutionContext();
        context.Accounts[creditCard.Id] = creditCard;
        context.Categories[category.Id] = category;
        var options = CreateOptions(context);
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Charge", 10m, category, null, creditCard);

        var json = JsonSerializer.Serialize(expense, CreateOptions());
        var deserialized = JsonSerializer.Deserialize<Expense>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }
}
