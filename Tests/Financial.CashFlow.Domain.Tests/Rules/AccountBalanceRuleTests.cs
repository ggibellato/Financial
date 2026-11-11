using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class AccountBalanceRuleTests
{
    private static readonly DateOnly StartDate = new(2026, 9, 1);

    private readonly Account _barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
    private readonly Account _chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly IncomeSource _salary = IncomeSource.Create("Gleison", IncomeGroup.Salary);

    public AccountBalanceRuleTests()
    {
        _barclays.SetOpeningBalance(1000m, StartDate);
        _chase.SetOpeningBalance(200m, StartDate);
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-06")]
    public void Balance_AsOfBeforeStartDate_ReturnsOpeningAmountOnly()
    {
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 8, 15), "Before the start", 50m, _groceries, _barclays, creditCard: null),
            Income.Create(new DateOnly(2026, 8, 20), _salary, null, 300m, _barclays)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 8, 31), records).Should().Be(1000m);
    }

    [Fact]
    public void Balance_RecordBeforeStartDate_IsIgnored()
    {
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 8, 31), "Day before the start", 50m, _groceries, _barclays, creditCard: null)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1000m);
    }

    [Fact]
    public void Balance_RecordOnStartDateAndOnAsOf_AreBothCounted()
    {
        var records = new IAccountTransaction[]
        {
            Income.Create(StartDate, _salary, null, 100m, _barclays),
            Income.Create(new DateOnly(2026, 9, 30), _salary, null, 10m, _barclays)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1110m);
    }

    [Fact]
    public void Balance_RecordAfterAsOf_IsIgnored()
    {
        var records = new IAccountTransaction[]
        {
            Income.Create(new DateOnly(2026, 10, 1), _salary, null, 100m, _barclays)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1000m);
    }

    [Fact]
    public void Balance_ExpenseWithRoundUp_SubtractsValuePlusRoundUp()
    {
        var expense = Expense.Create(new DateOnly(2026, 9, 5), "Coffee", 2.30m, _groceries, _chase, creditCard: null);
        expense.SetRoundUpAmount(0.70m);

        AccountBalanceRule.Balance(_chase, new DateOnly(2026, 9, 30), [expense]).Should().Be(197.00m);
    }

    [Fact]
    public void Balance_Transfer_LowersSourceAndRaisesDestination()
    {
        var records = new IAccountTransaction[]
        {
            Transfer.Create(new DateOnly(2026, 9, 10), _barclays, _chase, 150m, destinationAmount: null, note: null, today: TestClock.Today)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(850m);
        AccountBalanceRule.Balance(_chase, new DateOnly(2026, 9, 30), records).Should().Be(350m);
    }

    [Fact]
    public void Balance_CrossCurrencyTransfer_MovesDestinationByItsOwnAmount()
    {
        var itau = TestAccounts.Bank("Itaú", currency: Currency.BRL);
        itau.SetOpeningBalance(0m, StartDate);
        var records = new IAccountTransaction[]
        {
            Transfer.Create(new DateOnly(2026, 9, 10), _barclays, itau, 100m, destinationAmount: 640.50m, note: null, today: TestClock.Today)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(900m);
        AccountBalanceRule.Balance(itau, new DateOnly(2026, 9, 30), records).Should().Be(640.50m);
    }

    [Fact]
    public void Balance_Adjustment_AddsItsDelta()
    {
        var records = new IAccountTransaction[]
        {
            BalanceAdjustment.Create(new DateOnly(2026, 9, 12), _barclays, 990m, -10m, note: null)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(990m);
    }

    [Fact]
    public void Balance_RecordsOfAnotherAccount_DoNotCount()
    {
        var records = new IAccountTransaction[]
        {
            Income.Create(new DateOnly(2026, 9, 3), _salary, null, 500m, _chase),
            Expense.Create(new DateOnly(2026, 9, 4), "Groceries", 40m, _groceries, _chase, creditCard: null)
        };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1000m);
    }

    [Fact]
    public void Balance_AccountWithoutOpeningBalance_CountsEveryRecordUpToAsOf()
    {
        var account = Account.CreateWithCapabilities("Minimal", Currency.GBP, new HashSet<Capability> { Capability.ReceivesIncome });
        var records = new IAccountTransaction[]
        {
            Income.Create(new DateOnly(2017, 1, 1), _salary, null, 40m, account),
            Income.Create(new DateOnly(2026, 9, 30), _salary, null, 2m, account),
            Income.Create(new DateOnly(2026, 10, 1), _salary, null, 1000m, account)
        };

        AccountBalanceRule.Balance(account, new DateOnly(2026, 9, 30), records).Should().Be(42m);
    }

    [Fact]
    public void Balance_CardAccount_UnpaidChargeContributesMinusItsValue()
    {
        var card = TestAccounts.CreditCard("BA Amex");
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 9, 10), "Taxi", 30m, _groceries, null, card),
            Expense.Create(new DateOnly(2026, 9, 12), "Refund", -5m, _groceries, null, card)
        };

        AccountBalanceRule.Balance(card, new DateOnly(2026, 9, 30), records).Should().Be(-25m);
    }

    [Fact]
    public void Balance_CreditRefund_LowersTheCardAndRaisesTheBank()
    {
        var card = TestAccounts.CreditCard("BA Amex");
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 9, 12), "Refund", -60m, _groceries, null, card),
            Transfer.CreateCreditRefund(new DateOnly(2026, 9, 20), card, _barclays, 35m, null, new DateOnly(2026, 9, 30))
        };

        using (new AssertionScope())
        {
            AccountBalanceRule.Balance(card, new DateOnly(2026, 9, 30), records).Should().Be(25m);
            AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1035m);
        }
    }

    [Fact]
    public void Balance_SettledCharge_CountsOnItsBankOnlyAndNotOnTheCard()
    {
        var card = TestAccounts.CreditCard("BA Amex");
        var charge = Expense.Create(new DateOnly(2026, 9, 10), "Taxi", 30m, _groceries, null, card);
        charge.Settle(_barclays, new DateOnly(2026, 9, 25));
        var records = new IAccountTransaction[] { charge };

        AccountBalanceRule.Balance(card, new DateOnly(2026, 9, 30), records).Should().Be(0m);
        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(970m);
    }

    [Fact]
    public void Balance_UnpaidCharge_DoesNotMoveTheBankBalance()
    {
        var card = TestAccounts.CreditCard("BA Amex");
        var records = new IAccountTransaction[] { Expense.Create(new DateOnly(2026, 9, 10), "Taxi", 30m, _groceries, null, card) };

        AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1000m);
    }

    [Fact]
    public void Balance_CardAccount_ChargeDatedAfterAsOfIsLeftOut()
    {
        var card = TestAccounts.CreditCard("BA Amex");
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 9, 30), "Last day", 10m, _groceries, null, card),
            Expense.Create(new DateOnly(2026, 10, 1), "Next month", 99m, _groceries, null, card)
        };

        AccountBalanceRule.Balance(card, new DateOnly(2026, 9, 30), records).Should().Be(-10m);
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-02")]
    public void Balance_PotsAccount_SumsTheMovementsOfItsOwnPotsUpToAsOf()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var investimento = Pot.Create("Investimento", 50m);
        var ferias = Pot.Create("Ferias", 50m, isActive: false);
        reserva.AddPot(investimento);
        reserva.AddPot(ferias);
        var holidays = TestAccounts.Pots("Holidays");
        var flights = Pot.Create("Flights", 100m);
        holidays.AddPot(flights);
        var records = new IAccountTransaction[]
        {
            PotTransaction.Create(investimento, 100m, new DateOnly(2020, 1, 1), "First split"),
            PotTransaction.Create(ferias, -30m, new DateOnly(2026, 9, 30), "Withdrawal"),
            PotTransaction.Create(investimento, 40m, new DateOnly(2026, 10, 1), "Next month"),
            PotTransaction.Create(flights, 500m, new DateOnly(2026, 9, 1), "Other account")
        };

        using (new AssertionScope())
        {
            AccountBalanceRule.Balance(reserva, new DateOnly(2026, 9, 30), records).Should().Be(70m);
            AccountBalanceRule.Balance(holidays, new DateOnly(2026, 9, 30), records).Should().Be(500m);
            AccountBalanceRule.Balance(_barclays, new DateOnly(2026, 9, 30), records).Should().Be(1000m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-02")]
    public void PotBalance_CountsThatPotsMovementsOnOrBeforeAsOf()
    {
        var investimento = Pot.Create("Investimento", 50m);
        var ferias = Pot.Create("Ferias", 50m);
        var movements = new[]
        {
            PotTransaction.Create(investimento, 100m, new DateOnly(2026, 9, 1), "Split"),
            PotTransaction.Create(investimento, -25m, new DateOnly(2026, 9, 30), "On as-of"),
            PotTransaction.Create(investimento, 40m, new DateOnly(2026, 10, 1), "After as-of"),
            PotTransaction.Create(ferias, 70m, new DateOnly(2026, 9, 1), "Other pot")
        };

        AccountBalanceRule.PotBalance(investimento, new DateOnly(2026, 9, 30), movements).Should().Be(75m);
    }

    [Fact]
    public void PotBalance_InactivePotAndMaxAsOf_CountsEveryMovement()
    {
        var ferias = Pot.Create("Ferias", 0m, isActive: false);
        var movements = new[]
        {
            PotTransaction.Create(ferias, 100m, new DateOnly(2020, 1, 1), "Old split"),
            PotTransaction.Create(ferias, 20m, new DateOnly(2030, 1, 1), "Future-dated")
        };

        AccountBalanceRule.PotBalance(ferias, DateOnly.MaxValue, movements).Should().Be(120m);
    }

    [Fact]
    public void Balance_SettlementPerCurrency_SumsSignedAndConvertedValuesWithEmptyAsZero()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");
        var records = new IAccountTransaction[]
        {
            SettlementTransaction.Create(mae, new DateOnly(2026, 9, 2), "Paid by you", null, SettlementPayer.You, Currency.BRL, 300m, 45.5m, TestClock.Today),
            SettlementTransaction.Create(mae, new DateOnly(2026, 9, 3), "Paid by Mãe", null, SettlementPayer.Other, Currency.BRL, 100m, null, TestClock.Today),
            SettlementTransaction.Create(mae, new DateOnly(2026, 9, 4), "Pounds", null, SettlementPayer.You, Currency.GBP, 10m, 65m, TestClock.Today),
            SettlementTransaction.Create(mae, new DateOnly(2026, 10, 1), "After as-of", null, SettlementPayer.You, Currency.BRL, 999m, 99m, TestClock.Today),
            Expense.Create(new DateOnly(2026, 9, 5), "Another account", 50m, _groceries, _barclays, creditCard: null)
        };
        var asOf = new DateOnly(2026, 9, 30);

        using (new AssertionScope())
        {
            AccountBalanceRule.Balance(mae, Currency.BRL, asOf, records).Should().Be(265m);
            AccountBalanceRule.Balance(mae, Currency.GBP, asOf, records).Should().Be(55.5m);
            AccountBalanceRule.Balance(mae, asOf, records).Should().Be(265m);
        }
    }

    [Fact]
    public void Balance_OtherCurrencyThanTheMainOne_LeavesTheOpeningBalanceOut()
    {
        var records = new IAccountTransaction[]
        {
            Expense.Create(new DateOnly(2026, 9, 5), "Groceries", 50m, _groceries, _barclays, creditCard: null)
        };

        using (new AssertionScope())
        {
            AccountBalanceRule.Balance(_barclays, Currency.GBP, new DateOnly(2026, 9, 30), records).Should().Be(950m);
            AccountBalanceRule.Balance(_barclays, Currency.BRL, new DateOnly(2026, 9, 30), records).Should().Be(0m);
        }
    }

    [Fact]
    public void Balances_OneBalancePerAccountCurrencyInCodeOrder()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.GBP, Currency.BRL], "Mãe");
        var records = new IAccountTransaction[]
        {
            SettlementTransaction.Create(mae, new DateOnly(2026, 9, 2), "Pounds", null, SettlementPayer.Other, Currency.GBP, 10m, 65m, TestClock.Today)
        };

        AccountBalanceRule.Balances(mae, new DateOnly(2026, 9, 30), records)
            .Should().Equal((Currency.BRL, -65m), (Currency.GBP, -10m));
    }

    [Fact]
    public void Balances_ExistingBank_IsItsOneUnchangedBalance()
    {
        var records = new IAccountTransaction[]
        {
            Income.Create(new DateOnly(2026, 9, 20), _salary, null, 300m, _barclays),
            SettlementTransaction.Create(
                TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe"), new DateOnly(2026, 9, 2), "Entry", null, SettlementPayer.You, Currency.GBP, 10m, 65m, TestClock.Today)
        };

        AccountBalanceRule.Balances(_barclays, new DateOnly(2026, 9, 30), records).Should().Equal((Currency.GBP, 1300m));
    }

    [Fact]
    public void CurrentBalances_CountsRecordsOfEveryDate()
    {
        IAccountTransaction[] records =
        [
            Income.Create(new DateOnly(2026, 9, 3), _salary, null, 100m, _barclays),
            Income.Create(new DateOnly(2099, 12, 31), _salary, null, 5m, _barclays)
        ];

        AccountBalanceRule.CurrentBalances(_barclays, records).Should().Equal((Currency.GBP, 1105m));
    }
}
