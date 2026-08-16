using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Domain.Tests;

public class ExpenseTests
{
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly CreditCard ChaseMaster4023 = CreditCard.Create("ChaseMaster4023");
    private static readonly CreditCard BarclaysPlatinumVisa8003 = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard BaAmex = CreditCard.Create("BaAmex");
    private static readonly Category Casa = Category.Create("Casa");
    private static readonly Category Extras = Category.Create("Extras");
    private static readonly Category Mercado = Category.Create("Mercado");
    private static readonly Category Reserva = Category.Create("Reserva");
    private static readonly Category Investimento = Category.Create("Investimento", isInvestment: true);
    private static readonly Category Dizimo = Category.Create("Dizimo", isTithe: true);

    private static Expense CreateImmediateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Immediate", 10m, Casa, Chase, null);

    private static Expense CreateCardCharge() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Charge", 10m, Extras, null, ChaseMaster4023);

    private static Expense CreateSettledExpense()
    {
        var expense = CreateCardCharge();
        expense.Settle(Barclays, new DateOnly(2026, 7, 31));
        return expense;
    }

    // Expense.Create always populates ChargeDate for a card expense; a pre-F01 legacy record
    // (ChargeDate/InvoiceDate both null) can no longer be produced through the public API, so
    // this simulates that shape the same way deserialization of old data would.
    private static Expense SimulateLegacyCardExpense(Expense expense)
    {
        typeof(Expense).GetProperty(nameof(Expense.ChargeDate))!.SetMethod!.Invoke(expense, new object?[] { null });
        typeof(Expense).GetProperty(nameof(Expense.InvoiceDate))!.SetMethod!.Invoke(expense, new object?[] { null });
        return expense;
    }

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 15);

        var expense = Expense.Create(date, "Weekly groceries", 54.32m, Mercado, Barclays, null);

        using (new AssertionScope())
        {
            expense.Id.Should().NotBeEmpty();
            expense.Date.Should().Be(date);
            expense.Description.Should().Be("Weekly groceries");
            expense.Value.Should().Be(54.32m);
            expense.Category.Should().Be(Mercado);
            expense.PaymentSourceBank.Should().Be(Barclays);
            expense.CreditCard.Should().BeNull();
            expense.ChargeDate.Should().BeNull();
            expense.InvoiceDate.Should().BeNull();
        }
    }

    [Fact]
    public void Create_TwoExpenses_HaveDifferentIds()
    {
        var first = Expense.Create(new DateOnly(2026, 7, 1), "A", 1m, Casa, Chase, null);
        var second = Expense.Create(new DateOnly(2026, 7, 1), "B", 2m, Casa, Chase, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithPaymentSourceOnly_ComputesImmediatePayment()
    {
        var expense = CreateImmediateExpense();

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.ImmediatePayment);
    }

    [Fact]
    public void Create_WithCreditCardOnly_ComputesCreditCardCharge()
    {
        var expense = CreateCardCharge();

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        expense.PaymentSourceBank.Should().BeNull();
        expense.ChargeDate.Should().Be(expense.Date);
        expense.InvoiceDate.Should().Be(new DateOnly(expense.Date.Year, expense.Date.Month, 1));
    }

    [Fact]
    public void Create_ForCreditCardExpense_SetsChargeDateEqualToDate()
    {
        var date = new DateOnly(2026, 7, 15);

        var expense = Expense.Create(date, "Charge", 10m, Extras, null, ChaseMaster4023);

        expense.ChargeDate.Should().Be(date);
    }

    [Fact]
    public void Create_ForCreditCardExpense_DefaultsInvoiceDateToFirstOfChargeMonth()
    {
        var date = new DateOnly(2026, 7, 15);

        var expense = Expense.Create(date, "Charge", 10m, Extras, null, ChaseMaster4023);

        expense.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void Create_ForCreditCardExpense_WithExplicitInvoiceDateOverride_NormalizesToFirstOfThatMonth()
    {
        var date = new DateOnly(2026, 7, 28);
        var overrideInvoiceDate = new DateOnly(2026, 8, 17);

        var expense = Expense.Create(
            date, "Charge near cutoff", 10m, Extras, null, ChaseMaster4023, overrideInvoiceDate);

        expense.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
        expense.ChargeDate.Should().Be(date);
    }

    [Fact]
    public void Create_ForBankExpense_ChargeDateAndInvoiceDateAreNull()
    {
        var expense = CreateImmediateExpense();

        expense.ChargeDate.Should().BeNull();
        expense.InvoiceDate.Should().BeNull();
    }

    [Fact]
    public void Create_WithNeitherPaymentSourceNorCreditCard_Throws()
    {
        var act = () => Expense.Create(new DateOnly(2026, 7, 1), "Invalid", 10m, Casa, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public void Create_WithBothPaymentSourceAndCreditCard_Throws()
    {
        var act = () => Expense.Create(
            new DateOnly(2026, 7, 1),
            "Invalid",
            10m,
            Extras,
            Barclays,
            BarclaysPlatinumVisa8003);

        act.Should().Throw<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public void UpdateDetails_MutatesEveryFieldWithoutChangingId()
    {
        var expense = CreateImmediateExpense();
        var originalId = expense.Id;
        var newDate = new DateOnly(2026, 8, 1);

        expense.UpdateDetails(newDate, "Updated", 20m, Mercado, null, ChaseMaster4023);

        using (new AssertionScope())
        {
            expense.Id.Should().Be(originalId);
            expense.Date.Should().Be(newDate);
            expense.Description.Should().Be("Updated");
            expense.Value.Should().Be(20m);
            expense.Category.Should().Be(Mercado);
            expense.PaymentSourceBank.Should().BeNull();
            expense.CreditCard.Should().Be(ChaseMaster4023);
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        }
    }

    [Fact]
    public void Create_WithoutCountsAsTithe_DefaultsToTrue()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Tithe payment", 200m, Dizimo, Barclays, null);

        expense.CountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCountsAsTitheFalse_AssignsFalse()
    {
        var expense = Expense.Create(
            new DateOnly(2026, 7, 1), "Charitable offer", 50m, Dizimo, Barclays, null, countsAsTithe: false);

        expense.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_TogglingCountsAsTithe_ReplacesValue()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Tithe payment", 200m, Dizimo, Barclays, null);

        expense.UpdateDetails(expense.Date, expense.Description, expense.Value, Dizimo, Barclays, null, countsAsTithe: false);

        expense.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_WithoutCountsAsTithe_DefaultsToTrue()
    {
        var expense = Expense.Create(
            new DateOnly(2026, 7, 1), "Charitable offer", 50m, Dizimo, Barclays, null, countsAsTithe: false);

        expense.UpdateDetails(expense.Date, expense.Description, expense.Value, Dizimo, Barclays, null);

        expense.CountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithNeitherPaymentSourceNorCreditCard_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.UpdateDetails(expense.Date, "Updated", 20m, Casa, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public void UpdateDetails_WithBothPaymentSourceAndCreditCard_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.UpdateDetails(
            expense.Date, "Updated", 20m, Casa, Chase, BaAmex);

        act.Should().Throw<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_WithUnchangedPaymentFields_KeepsSettlement()
    {
        var expense = CreateSettledExpense();

        expense.UpdateDetails(expense.Date, "Renamed", 25m, Mercado, expense.PaymentSourceBank, expense.CreditCard);

        using (new AssertionScope())
        {
            expense.Description.Should().Be("Renamed");
            expense.Value.Should().Be(25m);
            expense.PaymentSourceBank.Should().Be(Barclays);
            expense.CreditCard.Should().Be(ChaseMaster4023);
            expense.Date.Should().Be(new DateOnly(2026, 7, 31));
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        }
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_ChangingPaymentFields_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.UpdateDetails(expense.Date, "Renamed", 25m, Mercado, Chase, expense.CreditCard);

        act.Should().Throw<ArgumentException>().WithMessage("*unmark its card statement paid*");
    }

    [Fact]
    public void Settle_OnCardCharge_SetsDateToPaymentDate_LeavesChargeDateAndInvoiceDateUnchanged()
    {
        var expense = CreateCardCharge();
        var originalChargeDate = expense.ChargeDate;
        var originalInvoiceDate = expense.InvoiceDate;
        var paymentDate = new DateOnly(2026, 7, 24);

        expense.Settle(Trading212, paymentDate);

        using (new AssertionScope())
        {
            expense.PaymentSourceBank.Should().Be(Trading212);
            expense.Date.Should().Be(paymentDate);
            expense.ChargeDate.Should().Be(originalChargeDate);
            expense.InvoiceDate.Should().Be(originalInvoiceDate);
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        }
    }

    [Fact]
    public void Settle_OnImmediatePayment_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.Settle(Barclays, new DateOnly(2026, 7, 24));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Settle_OnAlreadySettledExpense_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.Settle(Barclays, new DateOnly(2026, 7, 24));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Unsettle_OnSettledExpense_RevertsDateToChargeDate_ClearsPaymentSource()
    {
        var expense = CreateSettledExpense();

        expense.Unsettle();

        expense.PaymentSourceBank.Should().BeNull();
        expense.Date.Should().Be(expense.ChargeDate);
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }

    [Fact]
    public void Unsettle_OnUnsettledExpense_Throws()
    {
        var expense = CreateCardCharge();

        var act = () => expense.Unsettle();

        act.Should().Throw<ArgumentException>().WithMessage("*settled credit card expense*");
    }

    [Fact]
    public void SetInvoiceDate_WhileUnpaidCardCharge_UpdatesAndNormalizesToFirstOfMonth()
    {
        var expense = CreateCardCharge();

        expense.SetInvoiceDate(new DateOnly(2026, 8, 17));

        expense.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void SetInvoiceDate_WhenSettled_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.SetInvoiceDate(new DateOnly(2026, 8, 1));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void SetInvoiceDate_OnBankExpense_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.SetInvoiceDate(new DateOnly(2026, 8, 1));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void MigrateLegacyDates_OnUnpaidCharge_SetsChargeDateAndInvoiceDate_LeavesDateUnchanged()
    {
        var expense = SimulateLegacyCardExpense(CreateCardCharge());
        var originalDate = expense.Date;

        expense.MigrateLegacyDates(chargeDate: originalDate, invoiceDate: new DateOnly(2026, 7, 1), settledDate: null);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().Be(originalDate);
            expense.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
            expense.Date.Should().Be(originalDate);
        }
    }

    [Fact]
    public void MigrateLegacyDates_OnSettledExpenseWithSettledDate_SetsAllThreeFields()
    {
        var expense = SimulateLegacyCardExpense(CreateCardCharge());
        var originalChargeDate = expense.Date;
        typeof(Expense).GetProperty(nameof(Expense.PaymentSourceBank))!.SetMethod!.Invoke(expense, new object?[] { Barclays });
        var recoveredSettledAt = new DateOnly(2026, 8, 3);

        expense.MigrateLegacyDates(chargeDate: originalChargeDate, invoiceDate: new DateOnly(2026, 8, 1), settledDate: recoveredSettledAt);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().Be(originalChargeDate);
            expense.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
            expense.Date.Should().Be(recoveredSettledAt);
        }
    }

    [Fact]
    public void MigrateLegacyDates_OnBankExpense_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.MigrateLegacyDates(expense.Date, expense.Date, null);

        act.Should().Throw<ArgumentException>().WithMessage("*credit card expense*");
    }

    [Fact]
    public void MigrateLegacyDates_AlreadyMigrated_Throws()
    {
        var expense = CreateCardCharge();

        var act = () => expense.MigrateLegacyDates(expense.Date, expense.Date, null);

        act.Should().Throw<ArgumentException>().WithMessage("*already been migrated*");
    }

    [Fact]
    public void MigrateLegacyDates_SettledDateOnUnpaidCharge_Throws()
    {
        var expense = SimulateLegacyCardExpense(CreateCardCharge());

        var act = () => expense.MigrateLegacyDates(expense.Date, expense.Date, new DateOnly(2026, 8, 3));

        act.Should().Throw<ArgumentException>().WithMessage("*already-settled expense*");
    }

    [Theory]
    [InlineData(9.40, 0.60)]
    [InlineData(10.00, 0.00)]
    [InlineData(0.01, 0.99)]
    [InlineData(-9.40, 0.00)]
    [InlineData(-10.00, 0.00)]
    public void RoundUpSuggestion_ComputesDifferenceToNextWholePound(decimal value, decimal expected)
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test", value, Mercado, Chase, null);

        expense.RoundUpSuggestion.Should().Be(expected);
    }

    [Fact]
    public void SetRoundUpAmount_OnImmediatePaymentWithinRange_Succeeds()
    {
        var expense = CreateImmediateExpense();

        expense.SetRoundUpAmount(0.60m);

        expense.RoundUpAmount.Should().Be(0.60m);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.99)]
    public void SetRoundUpAmount_AtRangeBoundaries_Succeeds(decimal amount)
    {
        var expense = CreateImmediateExpense();

        expense.SetRoundUpAmount(amount);

        expense.RoundUpAmount.Should().Be(amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public void SetRoundUpAmount_OutsideRange_Throws(decimal amount)
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.SetRoundUpAmount(amount);

        act.Should().Throw<ArgumentException>().WithMessage("*between £0.00 and £0.99*");
    }

    [Fact]
    public void SetRoundUpAmount_OnNegativeValueExpense_Throws()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Reimbursement", -10m, Casa, Chase, null);

        var act = () => expense.SetRoundUpAmount(0.50m);

        act.Should().Throw<ArgumentException>().WithMessage("*negative (reimbursement) expense*");
    }

    [Fact]
    public void SetRoundUpAmount_OnCreditCardCharge_Throws()
    {
        var expense = CreateCardCharge();

        var act = () => expense.SetRoundUpAmount(0.50m);

        act.Should().Throw<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Fact]
    public void SetRoundUpAmount_OnSettledExpense_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.SetRoundUpAmount(0.50m);

        act.Should().Throw<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Fact]
    public void SetRoundUpAmount_Null_AlwaysSucceedsRegardlessOfShape()
    {
        var immediate = CreateImmediateExpense();
        var charge = CreateCardCharge();

        immediate.SetRoundUpAmount(null);
        charge.SetRoundUpAmount(null);

        immediate.RoundUpAmount.Should().BeNull();
        charge.RoundUpAmount.Should().BeNull();
    }

    [Fact]
    public void SetRoundUpAmount_Null_ClearsAPreviouslySetAmount()
    {
        var expense = CreateImmediateExpense();
        expense.SetRoundUpAmount(0.60m);

        expense.SetRoundUpAmount(null);

        expense.RoundUpAmount.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ChangingValue_LeavesRoundUpAmountUnchanged()
    {
        var expense = CreateImmediateExpense();
        expense.SetRoundUpAmount(0.60m);

        expense.UpdateDetails(expense.Date, expense.Description, 20m, expense.Category, expense.PaymentSourceBank, expense.CreditCard);

        expense.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public void IsInvestment_InvestimentoCategory_IsTrue()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Trading212 top-up", 100m, Investimento, Chase, null);

        expense.IsInvestment.Should().BeTrue();
    }

    public static IEnumerable<object[]> NonInvestmentCategories()
    {
        yield return new object[] { Mercado };
        yield return new object[] { Casa };
        yield return new object[] { Reserva };
    }

    [Theory]
    [MemberData(nameof(NonInvestmentCategories))]
    public void IsInvestment_OtherCategories_IsFalse(Category category)
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Not investment", 100m, category, Chase, null);

        expense.IsInvestment.Should().BeFalse();
    }

    [Fact]
    public void ReportingDate_ImmediatePayment_IsDate()
    {
        var expense = CreateImmediateExpense();

        expense.ReportingDate.Should().Be(expense.Date);
    }

    [Fact]
    public void ReportingDate_UnpaidCardCharge_IsInvoiceDate()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 28), "Charge near cutoff", 10m, Extras, null, ChaseMaster4023);

        expense.ReportingDate.Should().Be(expense.InvoiceDate);
        expense.ReportingDate.Should().NotBe(expense.Date);
    }

    [Fact]
    public void ReportingDate_SettledCardCharge_IsPostSettlementDate()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 28), "Charge near cutoff", 10m, Extras, null, ChaseMaster4023);
        expense.Settle(Barclays, new DateOnly(2026, 8, 5));

        expense.ReportingDate.Should().Be(expense.Date);
        expense.ReportingDate.Should().NotBe(expense.InvoiceDate);
    }

    [Fact]
    public void ReportingDate_LegacyUnpaidChargeWithoutInvoiceDate_FallsBackToDateInsteadOfThrowing()
    {
        var expense = SimulateLegacyCardExpense(CreateCardCharge());

        var act = () => expense.ReportingDate;

        act.Should().NotThrow();
        expense.ReportingDate.Should().Be(expense.Date);
    }
}
