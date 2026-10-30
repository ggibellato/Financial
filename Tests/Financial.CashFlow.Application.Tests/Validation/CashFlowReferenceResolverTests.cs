using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

[Trait("Category", "Unit")]
public class CashFlowReferenceResolverTests
{
    private readonly Account _reserva = TestAccounts.Pots("Reserva");
    private readonly Account[] _accounts;

    public CashFlowReferenceResolverTests()
    {
        _accounts = [TestAccounts.Bank("Barclays"), _reserva];
    }

    [Fact]
    public void ResolvePotsAccount_KnownId_ReturnsTheAccount()
    {
        var result = CashFlowReferenceResolver.ResolvePotsAccount(_accounts, _reserva.Id);

        result.Should().BeSameAs(_reserva);
    }

    [Fact]
    public void ResolvePotsAccount_NoId_ReturnsNull()
    {
        var result = CashFlowReferenceResolver.ResolvePotsAccount(_accounts, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolvePotsAccount_UnknownId_Throws()
    {
        var unknownId = Guid.NewGuid();

        var act = () => CashFlowReferenceResolver.ResolvePotsAccount(_accounts, unknownId);

        act.Should().Throw<ArgumentException>().WithMessage($"Pots account '{unknownId}' is not recognized.");
    }
}
