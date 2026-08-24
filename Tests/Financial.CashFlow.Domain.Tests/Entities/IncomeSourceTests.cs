using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class IncomeSourceTests
{
    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);

        using (new AssertionScope())
        {
            incomeSource.Id.Should().NotBeEmpty();
            incomeSource.Name.Should().Be("Gleison");
            incomeSource.Group.Should().Be(IncomeGroup.Salary);
            incomeSource.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_TwoIncomeSources_HaveDifferentIds()
    {
        var first = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var second = IncomeSource.Create("Ariana", IncomeGroup.Salary);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithIsActiveFalse_AssignsFalse()
    {
        var incomeSource = IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false);

        incomeSource.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => IncomeSource.Create(name!, IncomeGroup.Salary);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithoutAutoSplitToReserve_DefaultsToFalse()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);

        incomeSource.AutoSplitToReserve.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAutoSplitToReserveTrue_AssignsTrue()
    {
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true);

        incomeSource.AutoSplitToReserve.Should().BeTrue();
    }
}
