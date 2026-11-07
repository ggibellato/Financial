using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class SettlementTransactionTests
{
    private static readonly DateOnly EntryDate = new(2026, 7, 15);

    private static Account Mae() => TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

    private static Account Son() => TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var mae = Mae();

        var entry = SettlementTransaction.Create(mae, EntryDate, "School supplies", "Start of term", SettlementPayer.You, Currency.BRL, 350m, 51.23m);

        using (new AssertionScope())
        {
            entry.Id.Should().NotBeEmpty();
            entry.Account.Should().BeSameAs(mae);
            entry.Date.Should().Be(EntryDate);
            entry.Description.Should().Be("School supplies");
            entry.Note.Should().Be("Start of term");
            entry.PaidBy.Should().Be(SettlementPayer.You);
            entry.Currency.Should().Be(Currency.BRL);
            entry.Amount.Should().Be(350m);
            entry.ConvertedValue.Should().Be(51.23m);
        }
    }

    [Fact]
    public void Create_NullNote_StoresEmptyNote()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Medical appointment", null, SettlementPayer.Other, Currency.GBP, 40m, null);

        entry.Note.Should().BeEmpty();
        entry.ConvertedValue.Should().BeNull();
    }

    [Fact]
    public void Create_AccountWithoutParticipants_Refused()
    {
        var act = () => SettlementTransaction.Create(TestAccounts.Bank("Barclays"), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, null);

        act.Should().Throw<ArgumentException>().WithMessage("Barclays is not a Settlement account.");
    }

    [Fact]
    public void Create_CurrencyTheAccountDoesNotUse_Refused()
    {
        var act = () => SettlementTransaction.Create(Son(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, null);

        act.Should().Throw<ArgumentException>().WithMessage("Samuel doesn't use BRL.");
    }

    [Theory]
    [InlineData(10.005, 1.535, 10.01, 1.54)]
    [InlineData(486.3632, 72.05799207, 486.36, 72.06)]
    public void Create_ValuesWithMoreThanTwoDecimals_StoredRoundedToTwoAwayFromZero(decimal amount, decimal convertedValue, decimal expectedAmount, decimal expectedConvertedValue)
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, amount, convertedValue);

        using (new AssertionScope())
        {
            entry.Amount.Should().Be(expectedAmount);
            entry.ConvertedValue.Should().Be(expectedConvertedValue);
        }
    }

    [Theory]
    [InlineData(10.005, 2, 20.02)]
    [InlineData(350.125, 0.14627391, 51.21)]
    [InlineData(80, 6.138, 491.04)]
    public void ConvertedValueAt_RoundsTheAmountThenTheProductToTwoDecimals(decimal amount, decimal rate, decimal expected)
    {
        SettlementTransaction.ConvertedValueAt(amount, rate).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(0.004)]
    public void Create_AmountNotPositive_Refused(decimal amount)
    {
        var act = () => SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, amount, null);

        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Create_OneCurrencyAccount_RefusesAConvertedValue()
    {
        var act = () => SettlementTransaction.Create(Son(), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, 60m);

        act.Should().Throw<ArgumentException>().WithMessage("Samuel has one currency, so an entry has no converted value.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.005)]
    public void Create_NegativeConvertedValue_Refused(decimal convertedValue)
    {
        var act = () => SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, convertedValue);

        act.Should().Throw<ArgumentException>().WithMessage("The converted value must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.004)]
    [InlineData(-0.004)]
    public void Create_ConvertedValueRoundingToZero_StoredEmpty(decimal convertedValue)
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, convertedValue);

        entry.ConvertedValue.Should().BeNull();
    }

    [Fact]
    public void UpdateValues_ConvertedValueRoundingToZero_StoredEmpty()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, 2m);

        entry.UpdateValues(SettlementPayer.Other, 12m, 0.004m);

        entry.ConvertedValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.004, true)]
    [InlineData(-0.004, true)]
    [InlineData(0.005, false)]
    [InlineData(-0.005, false)]
    public void RoundsToZero_IsTrueWhenTheValueRoundsToZeroAtTwoDecimals(decimal value, bool expected)
    {
        SettlementTransaction.RoundsToZero(value).Should().Be(expected);
    }

    [Fact]
    public void Update_ChangesTheSixFieldsAndKeepsIdAccountAndCurrency()
    {
        var mae = Mae();
        var entry = SettlementTransaction.Create(mae, EntryDate, "Farmácia", "first", SettlementPayer.You, Currency.BRL, 150m, 22m);
        var originalId = entry.Id;

        entry.Update(new DateOnly(2026, 7, 14), "Farmácia e mercado", "metade", SettlementPayer.Other, 180.005m, 25.6m);

        using (new AssertionScope())
        {
            entry.Id.Should().Be(originalId);
            entry.Account.Should().BeSameAs(mae);
            entry.Currency.Should().Be(Currency.BRL);
            entry.Date.Should().Be(new DateOnly(2026, 7, 14));
            entry.Description.Should().Be("Farmácia e mercado");
            entry.Note.Should().Be("metade");
            entry.PaidBy.Should().Be(SettlementPayer.Other);
            entry.Amount.Should().Be(180.01m);
            entry.ConvertedValue.Should().Be(25.6m);
        }
    }

    [Fact]
    public void Update_NullNoteAndNullConvertedValue_StoreEmptyNoteAndNoConvertedValue()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", "note", SettlementPayer.You, Currency.BRL, 10m, 2m);

        entry.Update(EntryDate, "Entry", null, SettlementPayer.You, 10m, null);

        entry.Note.Should().BeEmpty();
        entry.ConvertedValue.Should().BeNull();
    }

    [Fact]
    public void Update_ConvertedValueRoundingToZero_StoredEmpty()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, 2m);

        entry.Update(EntryDate, "Entry", null, SettlementPayer.You, 10m, 0.001m);

        entry.ConvertedValue.Should().BeNull();
    }

    [Theory]
    [InlineData(" ", 10, null, "Description is required.")]
    [InlineData("New", 0, null, "Amount must be greater than zero.")]
    [InlineData("New", 10, -1, "The converted value must be greater than zero.")]
    public void Update_InvalidValue_RefusedWithoutChangingAnything(string description, decimal amount, int? convertedValue, string message)
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", "note", SettlementPayer.You, Currency.BRL, 10m, 2m);

        var act = () => entry.Update(new DateOnly(2026, 7, 1), description, "changed", SettlementPayer.Other, amount, convertedValue);

        act.Should().Throw<ArgumentException>().WithMessage(message);
        using (new AssertionScope())
        {
            entry.Date.Should().Be(EntryDate);
            entry.Description.Should().Be("Entry");
            entry.Note.Should().Be("note");
            entry.PaidBy.Should().Be(SettlementPayer.You);
            entry.Amount.Should().Be(10m);
            entry.ConvertedValue.Should().Be(2m);
        }
    }

    [Theory]
    [InlineData(60)]
    [InlineData(0)]
    [InlineData(0.004)]
    public void Update_ConvertedValueOnOneCurrencyAccount_Refused(decimal convertedValue)
    {
        var entry = SettlementTransaction.Create(Son(), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, null);

        var act = () => entry.Update(EntryDate, "Entry", null, SettlementPayer.You, 10m, convertedValue);

        act.Should().Throw<ArgumentException>().WithMessage("Samuel has one currency, so an entry has no converted value.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankDescription_Refused(string description)
    {
        var act = () => SettlementTransaction.Create(Mae(), EntryDate, description, null, SettlementPayer.You, Currency.BRL, 10m, null);

        act.Should().Throw<ArgumentException>().WithMessage("Description is required.");
    }

    [Fact]
    public void UpdateValues_ChangesPayerAndValuesWithoutChangingOtherFields()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Medical appointment", null, SettlementPayer.You, Currency.GBP, 40m, null);
        var originalId = entry.Id;

        entry.UpdateValues(SettlementPayer.Other, 45m, 320.50m);

        using (new AssertionScope())
        {
            entry.Id.Should().Be(originalId);
            entry.Date.Should().Be(EntryDate);
            entry.Currency.Should().Be(Currency.GBP);
            entry.PaidBy.Should().Be(SettlementPayer.Other);
            entry.Amount.Should().Be(45m);
            entry.ConvertedValue.Should().Be(320.50m);
        }
    }

    [Fact]
    public void UpdateValues_ValuesWithFourDecimals_StoredWithTwo()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 40m, null);

        entry.UpdateValues(SettlementPayer.Other, 7.9421m, 167.8768m);

        using (new AssertionScope())
        {
            entry.Amount.Should().Be(7.94m);
            entry.ConvertedValue.Should().Be(167.88m);
        }
    }

    [Fact]
    public void UpdateValues_InvalidAmount_RefusedAndKeepsTheStoredValues()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 10m, 2m);

        var act = () => entry.UpdateValues(SettlementPayer.Other, 0m, null);

        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.");
        entry.PaidBy.Should().Be(SettlementPayer.You);
        entry.Amount.Should().Be(10m);
        entry.ConvertedValue.Should().Be(2m);
    }

    [Fact]
    public void UpdateValues_ConvertedValueOnOneCurrencyAccount_Refused()
    {
        var entry = SettlementTransaction.Create(Son(), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, null);

        var act = () => entry.UpdateValues(SettlementPayer.You, 10m, 60m);

        act.Should().Throw<ArgumentException>().WithMessage("Samuel has one currency, so an entry has no converted value.");
    }

    [Theory]
    [InlineData(SettlementPayer.You, 1)]
    [InlineData(SettlementPayer.Other, -1)]
    public void SignedValueIn_SignFollowsPayerInBothCurrencies(SettlementPayer paidBy, int sign)
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, paidBy, Currency.GBP, 80m, 491.04m);

        using (new AssertionScope())
        {
            entry.SignedValueIn(Currency.GBP).Should().Be(sign * 80m);
            entry.SignedValueIn(Currency.BRL).Should().Be(sign * 491.04m);
        }
    }

    [Fact]
    public void SignedValueIn_EmptyConvertedValue_IsNullInTheOtherCurrency()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.Other, Currency.BRL, 100m, null);

        entry.SignedValueIn(Currency.BRL).Should().Be(-100m);
        entry.SignedValueIn(Currency.GBP).Should().BeNull();
    }

    [Fact]
    public void SignedValueIn_CurrencyTheAccountDoesNotHold_IsNull()
    {
        var mae = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 100m, 15m);
        var son = SettlementTransaction.Create(Son(), EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, null);

        mae.SignedValueIn(Currency.USD).Should().BeNull();
        son.SignedValueIn(Currency.BRL).Should().BeNull();
    }

    [Theory]
    [InlineData(5, SettlementPayer.You)]
    [InlineData(-5, SettlementPayer.Other)]
    public void PayerOf_SignSetsThePayer(int signedValue, SettlementPayer expected)
    {
        SettlementTransaction.PayerOf(signedValue).Should().Be(expected);
    }

    [Theory]
    [InlineData(10, null, true)]
    [InlineData(10, 2, true)]
    [InlineData(-10, -2, true)]
    [InlineData(0, null, false)]
    [InlineData(10, -2, false)]
    [InlineData(-10, 2, false)]
    [InlineData(10, 0, false)]
    public void HaveOneSign_NonZeroValueAndAnOtherValueOfTheSameSign(int signedValue, int? otherSignedValue, bool expected)
    {
        SettlementTransaction.HaveOneSign(signedValue, otherSignedValue).Should().Be(expected);
    }

    [Fact]
    public void ConvertedCurrencyOf_IsTheAccountsOtherCurrencyOrNone()
    {
        SettlementTransaction.ConvertedCurrencyOf(Mae(), Currency.GBP).Should().Be(Currency.BRL);
        SettlementTransaction.ConvertedCurrencyOf(Mae(), Currency.BRL).Should().Be(Currency.GBP);
        SettlementTransaction.ConvertedCurrencyOf(Son(), Currency.GBP).Should().BeNull();
    }

    [Fact]
    public void ContributionTo_OwnAccount_IsTheSignedValueInEachCurrency()
    {
        var mae = Mae();
        var entry = SettlementTransaction.Create(mae, EntryDate, "Entry", null, SettlementPayer.Other, Currency.GBP, 40m, 260.5m);

        using (new AssertionScope())
        {
            entry.ContributionTo(mae, Currency.GBP).Should().Be(-40m);
            entry.ContributionTo(mae, Currency.BRL).Should().Be(-260.5m);
            entry.ContributionTo(mae, Currency.USD).Should().Be(0m);
        }
    }

    [Fact]
    public void ContributionTo_EmptyConvertedValue_IsZeroInTheOtherCurrency()
    {
        var mae = Mae();
        var entry = SettlementTransaction.Create(mae, EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 100m, null);

        entry.ContributionTo(mae, Currency.GBP).Should().Be(0m);
    }

    [Fact]
    public void ContributionTo_AnotherAccount_IsZero()
    {
        var entry = SettlementTransaction.Create(Mae(), EntryDate, "Entry", null, SettlementPayer.You, Currency.BRL, 100m, 15m);
        var otherMae = Mae();

        using (new AssertionScope())
        {
            entry.ContributionTo(otherMae, Currency.BRL).Should().Be(0m);
            entry.ContributionTo(otherMae).Should().Be(0m);
            entry.ContributionTo(TestAccounts.Bank("Barclays")).Should().Be(0m);
        }
    }

    [Fact]
    public void ContributionTo_WithoutCurrency_IsTheContributionInTheAccountsMainCurrency()
    {
        var mae = Mae();
        var entry = SettlementTransaction.Create(mae, EntryDate, "Entry", null, SettlementPayer.You, Currency.GBP, 10m, 65m);

        entry.ContributionTo(mae).Should().Be(65m);
    }
}