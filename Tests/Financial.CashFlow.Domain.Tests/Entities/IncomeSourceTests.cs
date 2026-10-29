using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class IncomeSourceTests
{
    private static readonly Account Reserva = TestAccounts.Pots("Reserva");

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var incomeSource = IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false, potsAccount: Reserva);

        using (new AssertionScope())
        {
            incomeSource.Id.Should().NotBeEmpty();
            incomeSource.Name.Should().Be("Lottery");
            incomeSource.Group.Should().Be(IncomeGroup.NonReportable);
            incomeSource.IsActive.Should().BeFalse();
            incomeSource.PotsAccount.Should().BeSameAs(Reserva);
        }
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
    public void Create_WithoutPotsAccount_HasNone()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);

        incomeSource.PotsAccount.Should().BeNull();
    }

    [Fact]
    public void Create_WithAnAccountThatCannotReceiveAnIncomeSplit_Throws()
    {
        var act = () => IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: TestAccounts.Bank("Barclays"));

        act.Should().Throw<ArgumentException>().WithMessage("Barclays can't receive an income split.");
    }

    [Fact]
    public void Create_WithAnInactivePotsAccount_Throws()
    {
        var act = () => IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: TestAccounts.Pots("Old pots", isActive: false));

        act.Should().Throw<ArgumentException>().WithMessage("Old pots is inactive.");
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary, isActive: true);

        incomeSource.Update("Ariana", IncomeGroup.NonReportable, isActive: false, potsAccount: Reserva);

        using (new AssertionScope())
        {
            incomeSource.Name.Should().Be("Ariana");
            incomeSource.Group.Should().Be(IncomeGroup.NonReportable);
            incomeSource.IsActive.Should().BeFalse();
            incomeSource.PotsAccount.Should().BeSameAs(Reserva);
        }
    }

    [Fact]
    public void Update_WithoutPotsAccount_ClearsIt()
    {
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: Reserva);

        incomeSource.Update("Ariana", IncomeGroup.Salary, isActive: true, potsAccount: null);

        incomeSource.PotsAccount.Should().BeNull();
    }

    [Fact]
    public void Update_KeepingAPotsAccountThatHasSinceBeenDeactivated_Succeeds()
    {
        var holidays = TestAccounts.Pots("Holidays");
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: holidays);
        holidays.SetActive(false);

        incomeSource.Update("Ariana", IncomeGroup.Salary, isActive: true, potsAccount: holidays);

        incomeSource.PotsAccount.Should().BeSameAs(holidays);
    }

    [Fact]
    public void Update_ToAnInactivePotsAccount_ThrowsAndLeavesPriorValuesUntouched()
    {
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: Reserva);

        var act = () => incomeSource.Update("Ariana", IncomeGroup.Salary, isActive: true, potsAccount: TestAccounts.Pots("Old pots", isActive: false));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Old pots is inactive.");
            incomeSource.PotsAccount.Should().BeSameAs(Reserva);
        }
    }

    [Fact]
    public void Update_WithAnAccountThatCannotReceiveAnIncomeSplit_Throws()
    {
        var incomeSource = IncomeSource.Create("Ariana", IncomeGroup.Salary);

        var act = () => incomeSource.Update("Ariana", IncomeGroup.Salary, isActive: true, potsAccount: TestAccounts.Bank("Barclays"));

        act.Should().Throw<ArgumentException>().WithMessage("Barclays can't receive an income split.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);

        var act = () => incomeSource.Update(name!, IncomeGroup.NonReportable, isActive: false, potsAccount: Reserva);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            incomeSource.Name.Should().Be("Gleison");
            incomeSource.Group.Should().Be(IncomeGroup.Salary);
            incomeSource.IsActive.Should().BeTrue();
            incomeSource.PotsAccount.Should().BeNull();
        }
    }
}
