using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class TransferTests
{
    private static readonly DateOnly Today = TestClock.Today;
    private static readonly DateOnly Date = new(2026, 7, 25);

    private readonly Account _barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
    private readonly Account _trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: true);
    private readonly Account _chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
    private readonly Account _itau = TestAccounts.Bank("Itaú", currency: Currency.BRL);
    private readonly Account _closed = Account.CreateWithCapabilities(
        "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);

    private Transfer CreateValidTransfer() =>
        Transfer.Create(Date, _barclays, _trading212, 500m, destinationAmount: null, "Round-up top-up", Today);

    private Transfer CreateCrossCurrencyTransfer() =>
        Transfer.Create(Date, _barclays, _itau, 100m, destinationAmount: 640.50m, note: null, Today);

    private static void Deactivate(Account account) =>
        typeof(Account).GetProperty(nameof(Account.IsActive))!.SetValue(account, false);

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var transfer = Transfer.Create(Date, _barclays, _trading212, 500m, destinationAmount: null, "Round-up top-up", Today);

        using (new AssertionScope())
        {
            transfer.Id.Should().NotBeEmpty();
            transfer.Date.Should().Be(Date);
            transfer.SourceAccount.Should().Be(_barclays);
            transfer.DestinationAccount.Should().Be(_trading212);
            transfer.Amount.Should().Be(500m);
            transfer.DestinationAmount.Should().BeNull();
            transfer.Note.Should().Be("Round-up top-up");
        }
    }

    [Fact]
    public void Create_WithoutNote_AllowsNullNote()
    {
        var transfer = Transfer.Create(Date, _barclays, _trading212, 500m, destinationAmount: null, note: null, Today);

        transfer.Note.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutSource_Throws()
    {
        var act = () => Transfer.Create(Date, null!, _trading212, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Source account is required.");
    }

    [Fact]
    public void Create_WithoutDestination_Throws()
    {
        var act = () => Transfer.Create(Date, _barclays, null!, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Destination account is required.");
    }

    [Fact]
    public void Create_WithSameSourceAndDestination_Throws()
    {
        var act = () => Transfer.Create(Date, _barclays, _barclays, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Source and destination must be different accounts.");
    }

    [Fact]
    public void Create_PairNotAllowed_Throws()
    {
        var receiver = Account.CreateWithCapabilities("Receiver", Currency.GBP, new HashSet<Capability> { Capability.CanReceiveTransfers });

        var act = () => Transfer.Create(Date, receiver, _barclays, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfers from Receiver to Barclays are not allowed.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_InactiveAccount_Throws(bool inactiveSource)
    {
        var (source, destination) = inactiveSource ? (_closed, _barclays) : (_barclays, _closed);

        var act = () => Transfer.Create(Date, source, destination, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        var act = () => Transfer.Create(Date, _barclays, _trading212, amount, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfer amount must be greater than zero.");
    }

    [Fact]
    public void Create_CrossCurrencyWithoutDestinationAmount_Throws()
    {
        var act = () => Transfer.Create(Date, _barclays, _itau, 100m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Enter the amount received in BRL.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_CrossCurrencyWithNonPositiveDestinationAmount_Throws(decimal destinationAmount)
    {
        var act = () => Transfer.Create(Date, _barclays, _itau, 100m, destinationAmount, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Destination amount must be greater than zero.");
    }

    [Fact]
    public void Create_SameCurrencyWithDestinationAmount_Throws()
    {
        var act = () => Transfer.Create(Date, _barclays, _trading212, 100m, destinationAmount: 100m, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Destination amount only applies when the currencies differ.");
    }

    [Fact]
    public void Create_CrossCurrencyWithDestinationAmount_StoresIt()
    {
        var transfer = CreateCrossCurrencyTransfer();

        transfer.DestinationAmount.Should().Be(640.50m);
    }

    [Fact]
    public void Create_CrossCurrencyEqualAmounts_StoresDestinationAmount()
    {
        var transfer = Transfer.Create(Date, _barclays, _itau, 100m, destinationAmount: 100m, note: null, Today);

        using (new AssertionScope())
        {
            transfer.DestinationAmount.Should().Be(100m);
            transfer.ContributionTo(_itau).Should().Be(100m);
        }
    }

    [Fact]
    public void Create_DatedToday_Succeeds()
    {
        var transfer = Transfer.Create(Today, _barclays, _trading212, 500m, destinationAmount: null, note: null, Today);

        transfer.Date.Should().Be(Today);
    }

    [Fact]
    public void Create_DatedTomorrow_Throws()
    {
        var act = () => Transfer.Create(Today.AddDays(1), _barclays, _trading212, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Date must not be in the future.");
    }

    [Fact]
    public void Create_NoteOf200Characters_Succeeds()
    {
        var note = new string('a', 200);

        var transfer = Transfer.Create(Date, _barclays, _trading212, 500m, destinationAmount: null, note, Today);

        transfer.Note.Should().Be(note);
    }

    [Fact]
    public void Create_NoteOf201Characters_Throws()
    {
        var act = () => Transfer.Create(Date, _barclays, _trading212, 500m, destinationAmount: null, new string('a', 201), Today);

        act.Should().Throw<ArgumentException>().WithMessage("Note must be 200 characters or fewer.");
    }

    [Fact]
    public void Create_SeveralRulesBroken_ReportsTheFirstInRuleOrder()
    {
        var act = () => Transfer.Create(Today.AddDays(1), _barclays, _itau, 0m, destinationAmount: null, new string('a', 201), Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfer amount must be greater than zero.");
    }

    [Fact]
    public void UpdateDetails_MutatesEveryFieldWithoutChangingId()
    {
        var transfer = CreateValidTransfer();
        var originalId = transfer.Id;
        var newDate = new DateOnly(2026, 8, 1);

        transfer.UpdateDetails(newDate, _chase, _trading212, 250m, destinationAmount: null, "Updated note", Today);

        using (new AssertionScope())
        {
            transfer.Id.Should().Be(originalId);
            transfer.Date.Should().Be(newDate);
            transfer.SourceAccount.Should().Be(_chase);
            transfer.DestinationAccount.Should().Be(_trading212);
            transfer.Amount.Should().Be(250m);
            transfer.Note.Should().Be("Updated note");
        }
    }

    [Fact]
    public void UpdateDetails_WithSameSourceAndDestination_Throws()
    {
        var transfer = CreateValidTransfer();

        var act = () => transfer.UpdateDetails(transfer.Date, _chase, _chase, 100m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Source and destination must be different accounts.");
    }

    [Fact]
    public void UpdateDetails_WithNonPositiveAmount_Throws()
    {
        var transfer = CreateValidTransfer();

        var act = () => transfer.UpdateDetails(transfer.Date, _barclays, _trading212, 0m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfer amount must be greater than zero.");
    }

    [Fact]
    public void UpdateDetails_DatedTomorrow_ThrowsAndKeepsStoredValues()
    {
        var transfer = CreateValidTransfer();

        var act = () => transfer.UpdateDetails(Today.AddDays(1), _barclays, _trading212, 250m, destinationAmount: null, note: null, Today);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Date must not be in the future.");
            transfer.Date.Should().Be(Date);
            transfer.Amount.Should().Be(500m);
        }
    }

    [Fact]
    public void UpdateDetails_UnchangedInactiveAccount_Succeeds()
    {
        var transfer = CreateValidTransfer();
        Deactivate(_barclays);
        Deactivate(_trading212);

        transfer.UpdateDetails(Date, _barclays, _trading212, 250m, destinationAmount: null, "Corrected", Today);

        transfer.Amount.Should().Be(250m);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateDetails_SwitchToInactiveAccount_Throws(bool inactiveSource)
    {
        var transfer = CreateValidTransfer();
        var (source, destination) = inactiveSource ? (_closed, _trading212) : (_barclays, _closed);

        var act = () => transfer.UpdateDetails(Date, source, destination, 500m, destinationAmount: null, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Fact]
    public void UpdateDetails_OntoSameCurrencyPair_ClearsDestinationAmount()
    {
        var transfer = CreateCrossCurrencyTransfer();

        transfer.UpdateDetails(Date, _barclays, _chase, 100m, destinationAmount: null, note: null, Today);

        transfer.DestinationAmount.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_OntoSameCurrencyPairWithDestinationAmount_Throws()
    {
        var transfer = CreateCrossCurrencyTransfer();

        var act = () => transfer.UpdateDetails(Date, _barclays, _chase, 100m, destinationAmount: 640.50m, note: null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Destination amount only applies when the currencies differ.");
    }

    [Fact]
    public void AmountReceived_SameCurrencyTransfer_IsTheAmount()
    {
        CreateValidTransfer().AmountReceived().Should().Be(500m);
    }

    [Fact]
    public void AmountReceived_CrossCurrencyTransfer_IsTheDestinationAmount()
    {
        CreateCrossCurrencyTransfer().AmountReceived().Should().Be(640.50m);
    }

    [Fact]
    public void ContributionTo_SameCurrencyTransfer_MovesBothSidesByTheAmount()
    {
        var transfer = CreateValidTransfer();

        using (new AssertionScope())
        {
            transfer.ContributionTo(_barclays).Should().Be(-500m);
            transfer.ContributionTo(_trading212).Should().Be(500m);
            transfer.ContributionTo(_chase).Should().Be(0m);
        }
    }

    [Fact]
    public void ContributionTo_CrossCurrencyTransfer_CreditsTheDestinationAmount()
    {
        var transfer = CreateCrossCurrencyTransfer();

        using (new AssertionScope())
        {
            transfer.ContributionTo(_barclays).Should().Be(-100m);
            transfer.ContributionTo(_itau).Should().Be(640.50m);
        }
    }
}
