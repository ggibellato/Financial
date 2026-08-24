using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Text.Json;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowSerializerAdapterTests
{
    /// <summary>Every test drives the same CashFlowSerializerAdapter, so it is wired once here.</summary>
    private readonly CashFlowSerializerAdapter _sut;

    public CashFlowSerializerAdapterTests()
    {
        _sut = new CashFlowSerializerAdapter();
    }

    [Fact]
    public void SerializeThenDeserialize_RoundTripsAllCollectionsAndSharesReferenceInstances()
    {
        var original = CashFlowData.Create();
        var reserveBucket = ReserveBucket.Create("Investimento", 33.33m);
        var creditCard = CreditCard.Create("Barclays Platinum Visa 8003", isActive: true);
        var category = Category.Create("Investimento", isInvestment: true, isTithe: false, isActive: true);
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(1250.75m, new DateOnly(2026, 7, 1));
        var destinationBank = Bank.Create("Trading212", roundUpEnabled: true);
        var investmentAccount = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true);
        var expense = Expense.Create(
            new DateOnly(2026, 7, 15),
            "Weekly groceries",
            54.32m,
            category,
            null,
            creditCard);
        expense.Settle(bank, new DateOnly(2026, 7, 31));
        var income = Income.Create(new DateOnly(2026, 7, 25), incomeSource, 3200.00m, 2450.00m, bank, splitToReserve: true);
        var reserveMovement = ReserveMovement.Create(reserveBucket, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split", income);
        var cardStatement = CardStatement.Create(creditCard, 2026, 7);
        var recurringBill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1621m);
        var maeLedgerEntry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 15), "School supplies", "Note", Currency.BRL, 350m, 51.23m);
        var investmentSnapshot = InvestmentSnapshot.Create(investmentAccount, 2026, 7, 1250.00m);
        var transfer = Transfer.Create(new DateOnly(2026, 7, 25), bank, destinationBank, 500.00m, "Round-up top-up");
        var balanceAdjustment = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), bank, 2340.17m, -4.20m, "Matched against July statement");

        original.AddReserveBucket(reserveBucket);
        original.AddCreditCard(creditCard);
        original.AddCategory(category);
        original.AddExpense(expense);
        original.AddReserveMovement(reserveMovement);
        original.AddCardStatement(cardStatement);
        original.AddRecurringBill(recurringBill);
        original.AddMaeLedgerEntry(maeLedgerEntry);
        original.AddInvestmentSnapshot(investmentSnapshot);
        original.AddInvestmentAccount(investmentAccount);
        original.AddBank(bank);
        original.AddBank(destinationBank);
        original.AddIncomeSource(incomeSource);
        original.AddIncome(income);
        original.AddTransfer(transfer);
        original.AddBalanceAdjustment(balanceAdjustment);

        var json = _sut.Serialize(original);
        var result = _sut.Deserialize(json);

        using (new AssertionScope())
        {
            var resultReserveBucket = result.ReserveBuckets.Should().ContainSingle().Which;
            resultReserveBucket.Id.Should().Be(reserveBucket.Id);
            resultReserveBucket.Name.Should().Be(reserveBucket.Name);
            resultReserveBucket.IsActive.Should().Be(reserveBucket.IsActive);
            resultReserveBucket.SplitPercentage.Should().Be(reserveBucket.SplitPercentage);
            var resultExpense = result.Expenses.Should().ContainSingle().Which;
            resultExpense.Id.Should().Be(expense.Id);
            resultExpense.Date.Should().Be(expense.Date);
            resultExpense.Description.Should().Be(expense.Description);
            resultExpense.Value.Should().Be(expense.Value);
            resultExpense.Category.Id.Should().Be(expense.Category.Id);
            resultExpense.CreditCard.Should().NotBeNull();
            resultExpense.CreditCard!.Id.Should().Be(expense.CreditCard!.Id);
            resultExpense.ChargeDate.Should().Be(expense.ChargeDate);
            resultExpense.InvoiceDate.Should().Be(expense.InvoiceDate);
            var resultMovement = result.ReserveMovements.Should().ContainSingle().Which;
            resultMovement.Id.Should().Be(reserveMovement.Id);
            resultMovement.Bucket.Should().BeSameAs(resultReserveBucket);
            resultMovement.Amount.Should().Be(reserveMovement.Amount);
            resultMovement.Date.Should().Be(reserveMovement.Date);
            resultMovement.Description.Should().Be(reserveMovement.Description);
            var resultCardStatement = result.CardStatements.Should().ContainSingle().Which;
            resultCardStatement.Id.Should().Be(cardStatement.Id);
            result.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(recurringBill.Id);
            result.MaeLedgerEntries.Should().ContainSingle().Which.Id.Should().Be(maeLedgerEntry.Id);
            var resultSnapshot = result.InvestmentSnapshots.Should().ContainSingle().Which;
            resultSnapshot.Id.Should().Be(investmentSnapshot.Id);
            var resultInvestmentAccount = result.InvestmentAccounts.Should().ContainSingle().Which;
            resultInvestmentAccount.Id.Should().Be(investmentAccount.Id);
            resultInvestmentAccount.Name.Should().Be(investmentAccount.Name);
            resultInvestmentAccount.IsActive.Should().Be(investmentAccount.IsActive);
            resultInvestmentAccount.IsLiability.Should().Be(investmentAccount.IsLiability);
            var resultBanks = result.Banks.Should().HaveCount(2).And.Subject;
            var resultBank = resultBanks.Should().ContainSingle(b => b.Id == bank.Id).Subject;
            resultBank.Name.Should().Be(bank.Name);
            resultBank.RoundUpEnabled.Should().Be(bank.RoundUpEnabled);
            resultBank.OpeningBalance.Should().Be(bank.OpeningBalance);
            resultBank.OpeningBalanceDate.Should().Be(bank.OpeningBalanceDate);
            var resultDestinationBank = resultBanks.Should().ContainSingle(b => b.Id == destinationBank.Id).Subject;
            var resultIncomeSource = result.IncomeSources.Should().ContainSingle().Which;
            resultIncomeSource.Id.Should().Be(incomeSource.Id);
            resultIncomeSource.Name.Should().Be(incomeSource.Name);
            resultIncomeSource.IsActive.Should().Be(incomeSource.IsActive);
            resultIncomeSource.Group.Should().Be(incomeSource.Group);
            resultIncomeSource.AutoSplitToReserve.Should().Be(incomeSource.AutoSplitToReserve);
            var resultIncome = result.Incomes.Should().ContainSingle().Which;
            resultIncome.Id.Should().Be(income.Id);
            resultIncome.Date.Should().Be(income.Date);
            resultIncome.GrossValue.Should().Be(income.GrossValue);
            resultIncome.NetValue.Should().Be(income.NetValue);
            resultIncome.SplitToReserve.Should().Be(income.SplitToReserve);
            var resultTransfer = result.Transfers.Should().ContainSingle().Which;
            resultTransfer.Id.Should().Be(transfer.Id);
            resultTransfer.Date.Should().Be(transfer.Date);
            resultTransfer.Amount.Should().Be(transfer.Amount);
            resultTransfer.Note.Should().Be(transfer.Note);
            var resultBalanceAdjustment = result.BalanceAdjustments.Should().ContainSingle().Which;
            resultBalanceAdjustment.Id.Should().Be(balanceAdjustment.Id);
            resultBalanceAdjustment.Date.Should().Be(balanceAdjustment.Date);
            resultBalanceAdjustment.TargetBalance.Should().Be(balanceAdjustment.TargetBalance);
            resultBalanceAdjustment.Delta.Should().Be(balanceAdjustment.Delta);
            resultBalanceAdjustment.Note.Should().Be(balanceAdjustment.Note);
            var resultCreditCard = result.CreditCards.Should().ContainSingle().Which;
            resultCreditCard.Id.Should().Be(creditCard.Id);
            resultCreditCard.Name.Should().Be(creditCard.Name);
            resultCreditCard.IsActive.Should().Be(creditCard.IsActive);
            var resultCategory = result.Categories.Should().ContainSingle().Which;
            resultCategory.Id.Should().Be(category.Id);
            resultCategory.Name.Should().Be(category.Name);
            resultCategory.Active.Should().Be(category.Active);
            resultCategory.IsInvestment.Should().Be(category.IsInvestment);
            resultCategory.IsTithe.Should().Be(category.IsTithe);

            // Reference-equality: every reference-typed property must be the exact same instance
            // as the matching entry in its owning collection, not merely an equivalent copy.
            resultExpense.PaymentSourceBank.Should().BeSameAs(resultBank);
            resultIncome.IncomeSource.Should().BeSameAs(resultIncomeSource);
            resultIncome.Bank.Should().BeSameAs(resultBank);
            resultTransfer.SourceBank.Should().BeSameAs(resultBank);
            resultTransfer.DestinationBank.Should().BeSameAs(resultDestinationBank);
            resultBalanceAdjustment.Bank.Should().BeSameAs(resultBank);
            resultSnapshot.Account.Should().BeSameAs(resultInvestmentAccount);
            resultExpense.CreditCard.Should().BeSameAs(resultCreditCard);
            resultCardStatement.CreditCard.Should().BeSameAs(resultCreditCard);
            resultExpense.Category.Should().BeSameAs(resultCategory);
            resultMovement.Income.Should().BeSameAs(resultIncome);
        }
    }

    [Fact]
    public void Serialize_WritesOnlyIdsForReferenceTypedFields_NeverANestedObject()
    {
        var original = CashFlowData.Create();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var reserveBucket = Financial.CashFlow.Domain.Entities.ReserveBucket.Create("Investimento", 33.33m);
        original.AddBank(bank);
        original.AddIncomeSource(incomeSource);
        original.AddReserveBucket(reserveBucket);
        original.AddIncome(Income.Create(new DateOnly(2026, 7, 1), incomeSource, 100m, 90m, bank));
        original.AddReserveMovement(ReserveMovement.Create(reserveBucket, 50m, new DateOnly(2026, 7, 1), "Deposit"));

        var json = _sut.Serialize(original);

        using var document = JsonDocument.Parse(json);
        var income = document.RootElement.GetProperty("Incomes")[0];
        income.GetProperty("BankId").ValueKind.Should().Be(JsonValueKind.String);
        income.GetProperty("BankId").GetGuid().Should().Be(bank.Id);
        income.GetProperty("IncomeSourceId").ValueKind.Should().Be(JsonValueKind.String);
        income.GetProperty("IncomeSourceId").GetGuid().Should().Be(incomeSource.Id);
        income.TryGetProperty("Bank", out _).Should().BeFalse();
        income.TryGetProperty("IncomeSource", out _).Should().BeFalse();

        var movement = document.RootElement.GetProperty("ReserveMovements")[0];
        movement.GetProperty("BucketId").ValueKind.Should().Be(JsonValueKind.String);
        movement.GetProperty("BucketId").GetGuid().Should().Be(reserveBucket.Id);
        movement.TryGetProperty("Bucket", out _).Should().BeFalse();
    }

    [Fact]
    public void Deserialize_RegardlessOfOwningCollectionPositionInJsonText_ResolvesTheSameReference()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var bankJson = JsonSerializer.Serialize(new { bank.Id, bank.Name, bank.RoundUpEnabled, bank.OpeningBalance, bank.OpeningBalanceDate });
        // "Banks" appears AFTER "Incomes" in the text on purpose, to prove resolution doesn't
        // depend on encountering the owning collection first.
        var json = $$"""
            {
              "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Incomes": [{ "Id": "{{Guid.NewGuid()}}", "Date": "2026-07-01", "IncomeSourceId": "{{IncomeSourceIdForFixture}}", "GrossValue": 100, "NetValue": 90, "BankId": "{{bank.Id}}" }],
              "IncomeSources": [{ "Id": "{{IncomeSourceIdForFixture}}", "Name": "Gleison", "IsActive": true, "Group": "Salary" }],
              "Transfers": [], "BalanceAdjustments": [],
              "Banks": [{{bankJson}}]
            }
            """;

        var result = _sut.Deserialize(json);

        result.Incomes.Should().ContainSingle().Which.Bank!.Id.Should().Be(bank.Id);
    }

    private static readonly Guid IncomeSourceIdForFixture = Guid.NewGuid();

    [Fact]
    public void Deserialize_WithIdReferencingNoSeededBank_ThrowsDescriptiveException()
    {
        var missingBankId = Guid.NewGuid();
        var incomeId = Guid.NewGuid();
        var json = $$"""
            {
              "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Incomes": [{ "Id": "{{incomeId}}", "Date": "2026-07-01", "IncomeSourceId": "{{IncomeSourceIdForFixture}}", "GrossValue": 100, "NetValue": 90, "BankId": "{{missingBankId}}" }],
              "IncomeSources": [{ "Id": "{{IncomeSourceIdForFixture}}", "Name": "Gleison", "IsActive": true, "Group": "Salary" }],
              "Transfers": [], "BalanceAdjustments": [], "Banks": []
            }
            """;

        var act = () => _sut.Deserialize(json);

        act.Should().Throw<JsonException>().WithMessage($"*{missingBankId}*");
    }

    [Fact]
    public void Deserialize_ReserveMovementMissingIncomeIdKeyEntirely_DefaultsToUnlinked()
    {
        var bucketId = Guid.NewGuid();
        var json = $$"""
            {
              "Expenses": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Incomes": [], "IncomeSources": [], "Transfers": [], "BalanceAdjustments": [],
              "Banks": [], "CreditCards": [], "Categories": [],
              "ReserveBuckets": [{ "Id": "{{bucketId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 33.33 }],
              "ReserveMovements": [{ "Id": "{{Guid.NewGuid()}}", "BucketId": "{{bucketId}}", "Amount": 50, "Date": "2026-07-01", "Description": "Legacy manual movement" }]
            }
            """;

        var result = _sut.Deserialize(json);

        result.ReserveMovements.Should().ContainSingle().Which.Income.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReserveMovementWithIncomeIdReferencingAnIncomeDeserializedLater_ResolvesTheSameReference()
    {
        var incomeSourceId = Guid.NewGuid();
        var incomeId = Guid.NewGuid();
        var bucketId = Guid.NewGuid();
        // "ReserveMovements" appears BEFORE "Incomes" in the text on purpose, to prove
        // resolution doesn't depend on encountering Incomes first in the JSON text - only in the
        // converter's own read order (Incomes before ReserveMovements), which is independent of
        // JSON text order.
        var json = $$"""
            {
              "Expenses": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Transfers": [], "BalanceAdjustments": [], "Banks": [], "CreditCards": [], "Categories": [],
              "ReserveBuckets": [{ "Id": "{{bucketId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 33.33 }],
              "ReserveMovements": [{ "Id": "{{Guid.NewGuid()}}", "BucketId": "{{bucketId}}", "Amount": 50, "Date": "2026-07-01", "Description": "August salary", "IncomeId": "{{incomeId}}" }],
              "IncomeSources": [{ "Id": "{{incomeSourceId}}", "Name": "Ariana", "IsActive": true, "Group": "Salary", "AutoSplitToReserve": true }],
              "Incomes": [{ "Id": "{{incomeId}}", "Date": "2026-07-01", "IncomeSourceId": "{{incomeSourceId}}", "NetValue": 50, "BankId": null, "SplitToReserve": true }]
            }
            """;

        var result = _sut.Deserialize(json);

        result.ReserveMovements.Should().ContainSingle().Which.Income.Should().BeSameAs(result.Incomes.Should().ContainSingle().Which);
    }

    [Fact]
    public void Deserialize_LegacyRecordMissingTheIdReferenceField_FailsWithADescriptiveMessage()
    {
        var json = """
            {
              "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Incomes": [{ "Id": "11111111-1111-1111-1111-111111111111", "Date": "2026-07-01", "IncomeSource": "Gleison", "GrossValue": 100, "NetValue": 90, "Bank": "Barclays" }],
              "IncomeSources": [], "Transfers": [], "BalanceAdjustments": [], "Banks": []
            }
            """;

        var act = () => _sut.Deserialize(json);

        act.Should().Throw<JsonException>().WithMessage("*pre-migration string shape*reference migration*");
    }

    [Fact]
    public void Deserialize_LegacyReserveMovementMissingBucketId_FailsWithADescriptiveMessage()
    {
        var json = """
            {
              "Expenses": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "ReserveMovements": [{ "Id": "22222222-2222-2222-2222-222222222222", "Bucket": "Investimento", "Amount": 10, "Date": "2026-07-01", "Description": "Legacy" }],
              "Incomes": [], "IncomeSources": [], "Transfers": [], "BalanceAdjustments": [], "Banks": [], "ReserveBuckets": []
            }
            """;

        var act = () => _sut.Deserialize(json);

        act.Should().Throw<JsonException>().WithMessage("*pre-migration string shape*reference migration*");
    }

    [Fact]
    public void Serialize_ProducesCompactJsonWithoutIndentation()
    {
        var data = CashFlowData.Create();

        var json = _sut.Serialize(data);

        json.Should().NotContain("\n");
    }

    [Fact]
    public void SerializeThenDeserialize_WhenAllCollectionsEmpty_RoundTripsEmpty()
    {
        var original = CashFlowData.Create();

        var json = _sut.Serialize(original);
        var result = _sut.Deserialize(json);

        result.Expenses.Should().BeEmpty();
        result.ReserveMovements.Should().BeEmpty();
        result.CardStatements.Should().BeEmpty();
        result.RecurringBills.Should().BeEmpty();
        result.MaeLedgerEntries.Should().BeEmpty();
        result.InvestmentSnapshots.Should().BeEmpty();
        result.InvestmentAccounts.Should().BeEmpty();
        result.Banks.Should().BeEmpty();
        result.IncomeSources.Should().BeEmpty();
        result.ReserveBuckets.Should().BeEmpty();
        result.CreditCards.Should().BeEmpty();
        result.Categories.Should().BeEmpty();
        result.Incomes.Should().BeEmpty();
        result.Transfers.Should().BeEmpty();
        result.BalanceAdjustments.Should().BeEmpty();
    }
}
