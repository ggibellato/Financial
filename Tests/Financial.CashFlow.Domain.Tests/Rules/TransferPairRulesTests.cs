using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
[Trait("AC", "P60-F03-generic-account-to-account-transfer-09")]
public class TransferPairRulesTests
{
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Account _chase = TestAccounts.Bank("chase");
    private readonly Account _itau = Account.Create("Itaú", AccountTemplate.Bank, Currency.BRL);

    private static Account Minimal(string name, params Capability[] capabilities) =>
        Account.CreateWithCapabilities(name, Currency.GBP, capabilities.ToHashSet());

    [Fact]
    public void Allows_DefaultTable_BankToBankOnly()
    {
        var sender = Minimal("Sender", Capability.CanSendTransfers);
        var receiver = Minimal("Receiver", Capability.CanReceiveTransfers);

        using (new AssertionScope())
        {
            TransferPairRules.Default.Allows(_barclays, _chase).Should().BeTrue();
            TransferPairRules.Default.Allows(sender, receiver).Should().BeTrue();
            TransferPairRules.Default.Allows(receiver, sender).Should().BeFalse();
            TransferPairRules.Default.Allows(receiver, _chase).Should().BeFalse();
            TransferPairRules.Default.Allows(_barclays, sender).Should().BeFalse();
        }
    }

    [Fact]
    public void DestinationsFor_TableWithExtraPair_IncludesNewlyAllowedAccount()
    {
        var billPayee = Minimal("Bill payee", Capability.CanPayBills);
        var accounts = new[] { _barclays, _chase, billPayee };
        var extended = new TransferPairRules(
        [
            (Capability.CanSendTransfers, Capability.CanReceiveTransfers),
            (Capability.CanSendTransfers, Capability.CanPayBills)
        ]);

        using (new AssertionScope())
        {
            TransferPairRules.Default.DestinationsFor(_barclays, accounts).Should().Equal(_chase);
            extended.DestinationsFor(_barclays, accounts).Should().Equal(billPayee, _chase);
        }
    }

    [Fact]
    public void SourcesFor_TableWithExtraPair_IncludesNewlyAllowedAccount()
    {
        var reconciler = Minimal("Reconciler", Capability.CanReconcile);
        var accounts = new[] { _barclays, _chase, reconciler };
        var extended = new TransferPairRules(
        [
            (Capability.CanSendTransfers, Capability.CanReceiveTransfers),
            (Capability.CanReconcile, Capability.CanReceiveTransfers)
        ]);

        using (new AssertionScope())
        {
            TransferPairRules.Default.SourcesFor(_barclays, accounts).Should().Equal(_chase);
            extended.SourcesFor(_barclays, accounts).Should().Equal(_chase, reconciler);
        }
    }

    [Fact]
    public void DestinationsFor_InactiveAccountAndSelf_AreExcluded()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);

        var destinations = TransferPairRules.Default.DestinationsFor(_barclays, [_barclays, inactive, _itau, _chase]);

        destinations.Should().Equal(_chase, _itau);
    }

    [Fact]
    public void SourcesFor_InactiveAccountAndSelf_AreExcluded()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);

        var sources = TransferPairRules.Default.SourcesFor(_itau, [_itau, inactive, _chase, _barclays]);

        sources.Should().Equal(_barclays, _chase);
    }

    [Fact]
    public void DestinationsFor_AccountThatCannotSend_ReturnsEmpty()
    {
        var receiver = Minimal("Receiver", Capability.CanReceiveTransfers);

        TransferPairRules.Default.DestinationsFor(receiver, [_barclays, _chase]).Should().BeEmpty();
    }
}
