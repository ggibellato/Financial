using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class YearScopedInvestmentAccountResolverTests
{
    [Fact]
    public void ResolveForYear_CurrentYear_ReturnsOnlyActiveAccounts()
    {
        var active = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var disabled = InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false);
        var accounts = new[] { active, disabled };
        var snapshots = Array.Empty<InvestmentSnapshot>();

        var result = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, snapshots, year: 2026, currentYear: 2026);

        result.Should().ContainSingle().Which.Should().Be(active);
    }

    [Fact]
    public void ResolveForYear_PastYear_ReturnsOnlyAccountsWithASnapshotThatYear()
    {
        var withSnapshot = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var withoutSnapshot = InvestmentAccount.Create("Trading212Invested", isActive: true, isLiability: false);
        var accounts = new[] { withSnapshot, withoutSnapshot };
        var snapshots = new[] { InvestmentSnapshot.Create("ChaseSave", 2023, 5, 100m) };

        var result = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, snapshots, year: 2023, currentYear: 2026);

        result.Should().ContainSingle().Which.Should().Be(withSnapshot);
    }

    [Fact]
    public void ResolveForYear_PastYear_DisabledAccountWithSnapshotThatYear_StillIncluded()
    {
        var disabledButPresent = InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false);
        var accounts = new[] { disabledButPresent };
        var snapshots = new[] { InvestmentSnapshot.Create("EverydaySaver", 2020, 3, 50m) };

        var result = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, snapshots, year: 2020, currentYear: 2026);

        result.Should().ContainSingle().Which.Should().Be(disabledButPresent);
    }

    [Fact]
    public void ResolveForYear_PastYear_ActiveAccountWithNoSnapshotThatYear_IsExcluded()
    {
        var openedLater = InvestmentAccount.Create("Trading212Invested", isActive: true, isLiability: false);
        var accounts = new[] { openedLater };
        var snapshots = new[] { InvestmentSnapshot.Create("Trading212Invested", 2026, 1, 100m) };

        var result = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, snapshots, year: 2018, currentYear: 2026);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolveForYear_FutureYear_TreatedLikeCurrentYear()
    {
        var active = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var disabled = InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false);
        var accounts = new[] { active, disabled };

        var result = YearScopedInvestmentAccountResolver.ResolveForYear(accounts, Array.Empty<InvestmentSnapshot>(), year: 2027, currentYear: 2026);

        result.Should().ContainSingle().Which.Should().Be(active);
    }
}
