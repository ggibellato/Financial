using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowTypeInfoResolverTests
{
    private static JsonSerializerOptions CreateOptions(ReferenceResolutionContext? context = null) => new()
    {
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };

    [Fact]
    public void GetTypeInfo_ForManagedType_EnablesPrivateConstructor()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Expense), options);

        typeInfo!.CreateObject.Should().NotBeNull();
    }

    [Fact]
    public void GetTypeInfo_ForManagedType_LeavesReadOnlyComputedPropertyUnwired()
    {
        var options = CreateOptions();

        var typeInfo = options.TypeInfoResolver!.GetTypeInfo(typeof(Expense), options);

        // PaymentStatus is a computed (get-only) property with no excluded-properties mechanism
        // in this resolver, so it's still present in the JSON output but WirePropertySetter
        // can't find a setter for it.
        var paymentStatusProp = typeInfo!.Properties.Should().ContainSingle(p => p.Name == nameof(Expense.PaymentStatus)).Subject;
        paymentStatusProp.Set.Should().BeNull();
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
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var context = new ReferenceResolutionContext();
        context.Banks[bank.Id] = bank;
        var options = CreateOptions(context);
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Weekly groceries", 54.32m, Category.Mercado, bank, null);

        var json = JsonSerializer.Serialize(expense, CreateOptions());
        var deserialized = JsonSerializer.Deserialize<Expense>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(expense.Id);
        deserialized.Description.Should().Be("Weekly groceries");
        deserialized.Value.Should().Be(54.32m);
        deserialized.Category.Should().Be(Category.Mercado);
        deserialized.PaymentSourceBank.Should().BeSameAs(bank);
    }

    [Fact]
    public void GetTypeInfo_RoundTripsExpense_RecomputesPaymentStatusFromCreditCardAndPaymentSource()
    {
        // PaymentStatus is never in the JSON's Set path (it's excluded implicitly by having no
        // setter) - it must be recomputed correctly on deserialize from CreditCard/PaymentSource alone.
        var creditCard = CreditCard.Create("Chase Mastercard 4023");
        var context = new ReferenceResolutionContext();
        context.CreditCards[creditCard.Id] = creditCard;
        var options = CreateOptions(context);
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Charge", 10m, Category.Extras, null, creditCard);

        var json = JsonSerializer.Serialize(expense, CreateOptions());
        var deserialized = JsonSerializer.Deserialize<Expense>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }
}
