using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class WithdrawalCategoryRulesTests
{
    private static CategoryDTO Category(string name, bool active = true, bool isInvestment = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Active = active,
        IsInvestment = isInvestment,
        IsTithe = false,
        HasReferences = false,
    };

    [Fact]
    public void Eligible_ExcludesInactiveInvestmentAndReserva()
    {
        var result = WithdrawalCategoryRules.Eligible(
        [
            Category("Saude"),
            Category("Old", active: false),
            Category("Investimento", isInvestment: true),
            Category("Reserva"),
            Category("RESERVA"),
        ]);

        result.Select(c => c.Name).Should().Equal("Saude");
    }

    [Fact]
    public void DefaultFor_MatchesBucketNameCaseInsensitively()
    {
        var ariana = Category("Ariana");

        WithdrawalCategoryRules.DefaultFor([ariana, Category("Saude")], "ARIANA").Should().Be(ariana.Id);
    }

    [Fact]
    public void DefaultFor_NoMatch_ReturnsNull()
    {
        WithdrawalCategoryRules.DefaultFor([Category("Ariana")], "HouseTreats").Should().BeNull();
    }

    [Fact]
    public void DefaultFor_NullBucketName_ReturnsNull()
    {
        WithdrawalCategoryRules.DefaultFor([Category("Ariana")], null).Should().BeNull();
    }
}
