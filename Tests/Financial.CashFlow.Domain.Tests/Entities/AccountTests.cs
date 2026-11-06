using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class AccountTests
{
    private static Account CreateBank() => Account.Create("Chase", AccountTemplate.Bank, Currency.GBP);

    private static Account CreateMinimal(params Capability[] capabilities) =>
        Account.CreateWithCapabilities("Minimal", Currency.GBP, capabilities.ToHashSet());

    [Fact]
    public void Create_WithBankTemplate_HasAllEightCapabilitiesAndIsActive()
    {
        var account = CreateBank();

        using (new AssertionScope())
        {
            Enum.GetValues<Capability>().Count(account.Has).Should().Be(8);
            account.Has(Capability.HasStatements).Should().BeFalse();
            account.IsActive.Should().BeTrue();
            account.OpeningBalance.Should().Be(OpeningBalance.Create(0m, new DateOnly(1, 1, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(false));
        }
    }

    [Fact]
    public void Create_AssignsIdNameTemplateAndCurrency()
    {
        var account = Account.Create("Chase", AccountTemplate.Bank, Currency.GBP);

        using (new AssertionScope())
        {
            account.Id.Should().NotBeEmpty();
            account.Name.Should().Be("Chase");
            account.Template.Should().Be(AccountTemplate.Bank);
            account.Currency.Should().Be(Currency.GBP);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => Account.Create(name!, AccountTemplate.Bank, Currency.GBP);

        act.Should().Throw<ArgumentException>().WithMessage("Account name is required.");
    }

    [Fact]
    public void Create_WithUndefinedTemplate_Throws()
    {
        var act = () => Account.Create("Chase", (AccountTemplate)99, Currency.GBP);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithBankTemplate_IsNamedBankAndJoinsTheDefaultGroup()
    {
        var account = CreateBank();

        using (new AssertionScope())
        {
            account.TemplateName.Should().Be("Bank");
            account.JoinsDefaultGroup.Should().BeTrue();
            account.BalanceIsOwed.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(AccountTemplate.Bank, 0)]
    [InlineData(AccountTemplate.CreditCard, 1)]
    [InlineData(AccountTemplate.Pots, 2)]
    [InlineData(AccountTemplate.Settlement, 3)]
    public void TemplateOrder_IsTheTemplatesPositionInTheAvailableTemplates(AccountTemplate template, int expected)
    {
        Account.Create("Account", template, Currency.GBP).TemplateOrder.Should().Be(expected);
    }

    [Fact]
    public void CreateWithCapabilities_WithTemplateOutsideTheDefaultGroup_DoesNotJoinIt()
    {
        var account = Account.CreateWithCapabilities("Pots", Currency.GBP, new HashSet<Capability>(), template: (AccountTemplate)99);

        account.JoinsDefaultGroup.Should().BeFalse();
    }

    [Fact]
    public void CreateWithCapabilities_WithoutOpeningBalanceOrRoundUp_LeavesBothNull()
    {
        var account = CreateMinimal(Capability.ReceivesIncome);

        using (new AssertionScope())
        {
            account.OpeningBalance.Should().BeNull();
            account.RoundUp.Should().BeNull();
            account.Has(Capability.ReceivesIncome).Should().BeTrue();
            account.Has(Capability.HasCategories).Should().BeFalse();
        }
    }

    [Fact]
    public void SetOpeningBalance_ReplacesAmountAndStartDate()
    {
        var account = CreateBank();

        account.SetOpeningBalance(1250.75m, new DateOnly(2026, 7, 1));

        account.OpeningBalance.Should().Be(OpeningBalance.Create(1250.75m, new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void SetOpeningBalance_Negative_ThrowsArgumentException()
    {
        var account = CreateBank();
        account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));

        var act = () => account.SetOpeningBalance(-1m, new DateOnly(2026, 8, 1));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
            account.OpeningBalance.Should().Be(OpeningBalance.Create(100m, new DateOnly(2026, 7, 1)));
        }
    }

    [Fact]
    public void SetOpeningBalance_WithoutCapability_ThrowsArgumentException()
    {
        var account = CreateMinimal(Capability.SupportsRoundUp);

        var act = () => account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Minimal has no opening balance.");
            account.OpeningBalance.Should().BeNull();
        }
    }

    [Fact]
    public void SetOpeningBalance_WithOnlyThatCapability_Succeeds()
    {
        var account = CreateMinimal(Capability.HasOpeningBalance);

        account.SetOpeningBalance(10m, new DateOnly(2026, 9, 1));

        account.OpeningBalance.Should().Be(OpeningBalance.Create(10m, new DateOnly(2026, 9, 1)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetRoundUp_ReplacesTheSetting(bool enabled)
    {
        var account = CreateBank();

        account.SetRoundUp(enabled);

        account.RoundUp.Should().Be(new RoundUpSetting(enabled));
    }

    [Fact]
    public void SetRoundUp_WithoutCapability_ThrowsArgumentException()
    {
        var account = CreateMinimal(Capability.HasOpeningBalance);

        var act = () => account.SetRoundUp(true);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Minimal does not support round-up.");
            account.RoundUp.Should().BeNull();
        }
    }

    [Fact]
    public void SetRoundUp_WithOnlyThatCapability_Succeeds()
    {
        var account = CreateMinimal(Capability.SupportsRoundUp);

        account.SetRoundUp(true);

        account.RoundUp.Should().Be(new RoundUpSetting(true));
    }

    [Fact]
    public void Update_ChangesNameAndKeepsOpeningBalanceAndRoundUp()
    {
        var account = CreateBank();
        account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        account.SetRoundUp(true);

        account.Update("Barclays");

        using (new AssertionScope())
        {
            account.Name.Should().Be("Barclays");
            account.OpeningBalance.Should().Be(OpeningBalance.Create(100m, new DateOnly(2026, 7, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(true));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesNameUntouched(string? name)
    {
        var account = CreateBank();

        var act = () => account.Update(name!);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Account name is required.");
            account.Name.Should().Be("Chase");
        }
    }

    [Fact]
    public void Create_TrimsTheName()
    {
        var account = Account.Create("  Monzo  ", AccountTemplate.Bank, Currency.GBP);

        account.Name.Should().Be("Monzo");
    }

    [Fact]
    public void Create_WithASixtyCharacterName_Succeeds()
    {
        var name = new string('a', 60);

        var account = Account.Create(name, AccountTemplate.Bank, Currency.GBP);

        account.Name.Should().Be(name);
    }

    [Fact]
    public void Create_WithASixtyOneCharacterName_Throws()
    {
        var act = () => Account.Create(new string('a', 61), AccountTemplate.Bank, Currency.GBP);

        act.Should().Throw<ArgumentException>().WithMessage("Account name must be 60 characters or fewer.");
    }

    [Theory]
    [InlineData(Currency.BRL)]
    [InlineData(Currency.USD)]
    public void Create_BankInAnotherCurrency_Throws(Currency currency)
    {
        var act = () => Account.Create("Itaú", AccountTemplate.Bank, currency);

        act.Should().Throw<ArgumentException>().WithMessage("Bank accounts must be GBP.");
    }

    [Fact]
    public void CreateWithCapabilities_InAnyCurrency_Succeeds()
    {
        var account = Account.CreateWithCapabilities("Itaú", Currency.BRL, new HashSet<Capability>());

        account.Currency.Should().Be(Currency.BRL);
    }

    [Fact]
    public void Update_TrimsTheName()
    {
        var account = CreateBank();

        account.Update(" Barclays ");

        account.Name.Should().Be("Barclays");
    }

    [Fact]
    public void Update_WithASixtyOneCharacterName_ThrowsAndLeavesNameUntouched()
    {
        var account = CreateBank();

        var act = () => account.Update(new string('a', 61));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Account name must be 60 characters or fewer.");
            account.Name.Should().Be("Chase");
        }
    }

    [Fact]
    public void SetActive_DeactivatesAndReactivates()
    {
        var account = CreateBank();

        account.SetActive(false);
        var afterDeactivate = account.IsActive;
        account.SetActive(true);

        using (new AssertionScope())
        {
            afterDeactivate.Should().BeFalse();
            account.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public void ChangeTemplate_ToATemplateWithMoreCapabilities_DefaultsTheGainedFields()
    {
        var account = CreateMinimal(Capability.ReceivesIncome);

        account.ChangeTemplate(AccountTemplate.Bank, Currency.GBP);

        using (new AssertionScope())
        {
            account.Template.Should().Be(AccountTemplate.Bank);
            Enum.GetValues<Capability>().Count(account.Has).Should().Be(8);
            account.OpeningBalance.Should().Be(OpeningBalance.Create(0m, default));
            account.RoundUp.Should().Be(new RoundUpSetting(false));
        }
    }

    [Fact]
    public void ChangeTemplate_ToAnUndefinedTemplate_ThrowsAndLeavesTheAccountUntouched()
    {
        var account = Account.CreateWithCapabilities(
            "Pots", Currency.GBP, new HashSet<Capability> { Capability.HasOpeningBalance, Capability.SupportsRoundUp });

        var act = () => account.ChangeTemplate((AccountTemplate)99, Currency.GBP);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Account template '99' is not defined.");
            account.Has(Capability.ReceivesIncome).Should().BeFalse();
            account.OpeningBalance.Should().NotBeNull();
        }
    }

    [Fact]
    public void ChangeTemplate_ToTheSameTemplate_KeepsOpeningBalanceAndRoundUp()
    {
        var account = CreateBank();
        account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        account.SetRoundUp(true);

        account.ChangeTemplate(AccountTemplate.Bank, Currency.GBP);

        using (new AssertionScope())
        {
            account.OpeningBalance.Should().Be(OpeningBalance.Create(100m, new DateOnly(2026, 7, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(true));
        }
    }

    [Fact]
    public void ChangeTemplate_ToANotAllowedCurrency_ThrowsAndLeavesTheAccountUntouched()
    {
        var account = CreateBank();

        var act = () => account.ChangeTemplate(AccountTemplate.Bank, Currency.BRL);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Bank accounts must be GBP.");
            account.Currency.Should().Be(Currency.GBP);
        }
    }

    [Fact]
    public void Create_WithCreditCardTemplate_HasTheCardCapabilitiesAndNoOpeningBalanceOrRoundUp()
    {
        var account = Account.Create("BA Amex", AccountTemplate.CreditCard, Currency.GBP);

        using (new AssertionScope())
        {
            account.Has(Capability.HasStatements).Should().BeTrue();
            account.Has(Capability.HasCategories).Should().BeTrue();
            account.Has(Capability.ReceivesPayments).Should().BeTrue();
            account.Has(Capability.CanPayBills).Should().BeFalse();
            account.OpeningBalance.Should().BeNull();
            account.RoundUp.Should().BeNull();
            account.JoinsDefaultGroup.Should().BeTrue();
            account.BalanceIsOwed.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_CreditCardInAnotherCurrency_Throws()
    {
        var act = () => Account.Create("BA Amex", AccountTemplate.CreditCard, Currency.BRL);

        act.Should().Throw<ArgumentException>().WithMessage("CreditCard accounts must be GBP.");
    }

    [Fact]
    public void Pots_PotsAccount_StartsEmpty_OtherTemplatesHaveNone()
    {
        using (new AssertionScope())
        {
            Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP).Pots.Should().BeEmpty();
            CreateBank().Pots.Should().BeNull();
            Account.Create("BA Amex", AccountTemplate.CreditCard, Currency.GBP).Pots.Should().BeNull();
        }
    }

    [Fact]
    public void AddPot_PotsAccount_KeepsThePotsInOrderAndOwnsThem()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        var investimento = Pot.Create("Investimento", 50m);
        var ariana = Pot.Create("Ariana", 50m);

        reserva.AddPot(investimento);
        reserva.AddPot(ariana);

        using (new AssertionScope())
        {
            reserva.Pots.Should().Equal(investimento, ariana);
            reserva.OwnsPot(ariana.Id).Should().BeTrue();
            reserva.OwnsPot(Guid.NewGuid()).Should().BeFalse();
        }
    }

    [Fact]
    public void AddPot_AccountWithoutPots_Throws()
    {
        var act = () => CreateBank().AddPot(Pot.Create("Investimento", 50m));

        act.Should().Throw<ArgumentException>().WithMessage("Chase has no pots.");
    }

    [Fact]
    public void AddPot_NameAlreadyUsedInAnyCase_ThrowsAndKeepsThePots()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        reserva.AddPot(Pot.Create("Investimento", 50m));

        var act = () => reserva.AddPot(Pot.Create("INVESTIMENTO", 10m));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("A pot named \"INVESTIMENTO\" already exists.");
            reserva.Pots.Should().ContainSingle();
        }
    }

    private static (Account Reserva, Pot Investimento, Pot Ariana) ReservaWithTwoPots()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        var investimento = Pot.Create("Investimento", 60m);
        var ariana = Pot.Create("Ariana", 40m);
        reserva.AddPot(investimento);
        reserva.AddPot(ariana);
        return (reserva, investimento, ariana);
    }

    private static void ShouldKeepThePots(Account reserva, Pot investimento, Pot ariana)
    {
        reserva.Pots.Should().Equal(investimento, ariana);
        (investimento.Name, investimento.SplitPercentage, investimento.IsActive).Should().Be(("Investimento", 60m, true));
        (ariana.Name, ariana.SplitPercentage, ariana.IsActive).Should().Be(("Ariana", 40m, true));
    }

    [Fact]
    public void ReplacePots_RenameSplitDeactivateAndAdd_AppliesAllAndKeepsExistingOrder()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        reserva.ReplacePots(
        [
            new PotChange(null, " Holidays ", 16.67m, true),
            new PotChange(ariana.Id, " Ari ", 33.33m, true),
            new PotChange(investimento.Id, "Investimento", 50m, true),
            new PotChange(null, "Old", 10m, false)
        ]);

        using (new AssertionScope())
        {
            reserva.Pots!.Select(pot => (pot.Name, pot.SplitPercentage, pot.IsActive)).Should().Equal(
                ("Investimento", 50m, true), ("Ari", 33.33m, true), ("Holidays", 16.67m, true), ("Old", 10m, false));
            reserva.Pots![0].Should().BeSameAs(investimento);
            reserva.Pots![1].Should().BeSameAs(ariana);
        }
    }

    [Fact]
    public void ReplacePots_SwappingTwoNames_IsAllowed()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        reserva.ReplacePots([new PotChange(investimento.Id, "Ariana", 60m, true), new PotChange(ariana.Id, "Investimento", 40m, true)]);

        reserva.Pots!.Select(pot => pot.Name).Should().Equal("Ariana", "Investimento");
    }

    [Theory]
    [InlineData(55, 40, "Active pots must total 100% (now 95%).")]
    [InlineData(59.99, 40, "Active pots must total 100% (now 99.99%).")]
    [InlineData(60, 40.5, "Active pots must total 100% (now 100.5%).")]
    [InlineData(60.004, 40, "Active pots must total 100% (now 100.004%).")]
    public void ReplacePots_ActiveSplitNot100_ThrowsAndChangesNothing(double first, double second, string message)
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots(
        [
            new PotChange(investimento.Id, "Renamed", (decimal)first, true),
            new PotChange(ariana.Id, "Ariana", (decimal)second, true)
        ]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage(message);
            ShouldKeepThePots(reserva, investimento, ariana);
        }
    }

    [Fact]
    public void ReplacePots_EveryPotInactive_Throws()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots([new PotChange(investimento.Id, "Investimento", 60m, false), new PotChange(ariana.Id, "Ariana", 40m, false)]);

        act.Should().Throw<ArgumentException>().WithMessage("Active pots must total 100% (now 0%).");
    }

    [Fact]
    public void ReplacePots_InactivePotsSplit_DoesNotCountTowardTheTotal()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        reserva.ReplacePots([new PotChange(investimento.Id, "Investimento", 100m, true), new PotChange(ariana.Id, "Ariana", 40m, false)]);

        ariana.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ReplacePots_PotMissingFromTheList_ThrowsAndChangesNothing()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots([new PotChange(investimento.Id, "Renamed", 100m, true)]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Every pot must be in the list; pots can't be deleted.");
            ShouldKeepThePots(reserva, investimento, ariana);
        }
    }

    [Fact]
    public void ReplacePots_PotListedTwice_Throws()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots(
        [
            new PotChange(investimento.Id, "One", 30m, true),
            new PotChange(investimento.Id, "Two", 30m, true),
            new PotChange(ariana.Id, "Ariana", 40m, true)
        ]);

        act.Should().Throw<ArgumentException>().WithMessage("Every pot must be in the list; pots can't be deleted.");
    }

    [Fact]
    public void ReplacePots_PotOfAnotherAccount_Throws()
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots(
        [
            new PotChange(investimento.Id, "Investimento", 60m, true),
            new PotChange(ariana.Id, "Ariana", 40m, true),
            new PotChange(Guid.NewGuid(), "Foreign", 0m, false)
        ]);

        act.Should().Throw<ArgumentException>().WithMessage("That pot isn't in Reserva.");
    }

    [Theory]
    [InlineData("ARIANA")]
    [InlineData(" ariana ")]
    public void ReplacePots_DuplicateNameInAnyCase_ThrowsAndChangesNothing(string newName)
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots(
        [
            new PotChange(investimento.Id, "Investimento", 60m, true),
            new PotChange(ariana.Id, "Ariana", 40m, true),
            new PotChange(null, newName, 0m, false)
        ]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("A pot named \"Ariana\" already exists.");
            ShouldKeepThePots(reserva, investimento, ariana);
        }
    }

    [Theory]
    [InlineData("  ", 40, "Pot name is required.")]
    [InlineData("Ariana", 101, "Split percentage must be between 0 and 100.")]
    [InlineData("Ariana", -1, "Split percentage must be between 0 and 100.")]
    public void ReplacePots_InvalidPotAfterAValidOne_ThrowsAndChangesNothing(string name, double split, string message)
    {
        var (reserva, investimento, ariana) = ReservaWithTwoPots();

        var act = () => reserva.ReplacePots(
        [
            new PotChange(investimento.Id, "Renamed", 60m, true),
            new PotChange(ariana.Id, name, (decimal)split, true)
        ]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage(message);
            ShouldKeepThePots(reserva, investimento, ariana);
        }
    }

    [Fact]
    public void ReplacePots_AccountWithoutPots_Throws()
    {
        var act = () => CreateBank().ReplacePots([]);

        act.Should().Throw<ArgumentException>().WithMessage("Chase has no pots.");
    }

    [Fact]
    public void ChangeTemplate_AwayFromPots_DropsThePots()
    {
        var account = Account.Create("Holiday", AccountTemplate.Pots, Currency.GBP);
        account.AddPot(Pot.Create("Trip", 100m));

        account.ChangeTemplate(AccountTemplate.Bank, Currency.GBP);

        using (new AssertionScope())
        {
            account.Pots.Should().BeNull();
            account.ChangeTemplate(AccountTemplate.Pots, Currency.GBP);
            account.Pots.Should().BeEmpty();
        }
    }

    [Fact]
    public void SetCategory_PotsAccount_SetsIt()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        var category = Category.Create("Reserva");

        reserva.SetCategory(category);

        reserva.Category.Should().BeSameAs(category);
    }

    [Fact]
    public void SetCategory_AccountWithoutPots_ThrowsAndLeavesItUnset()
    {
        var bank = CreateBank();

        var act = () => bank.SetCategory(Category.Create("Reserva"));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Chase has no category setting.");
            bank.Category.Should().BeNull();
        }
    }

    [Fact]
    public void ChangeTemplate_AwayFromPots_DropsTheCategory()
    {
        var account = Account.Create("Holiday", AccountTemplate.Pots, Currency.GBP);
        account.SetCategory(Category.Create("Holidays"));

        account.ChangeTemplate(AccountTemplate.Bank, Currency.GBP);

        account.Category.Should().BeNull();
    }
}
