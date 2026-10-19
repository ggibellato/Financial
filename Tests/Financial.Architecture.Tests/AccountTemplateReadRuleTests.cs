using System.Reflection;
using Financial.Architecture.Tests.Fixtures;
using Financial.Architecture.Tests.Infrastructure;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Architecture.Tests;

[Trait("Category", "Integration")]
public class AccountTemplateReadRuleTests
{
    private const string TemplateGetter = "Financial.CashFlow.Domain.Entities.Account::get_Template(";

    private static readonly IReadOnlyDictionary<string, string> AllowList = new Dictionary<string, string>
    {
        [typeof(Account).FullName!] = "Capabilities come from the account's own template",
        [typeof(BankService).FullName!] = "Removed in F13 with /banks",
        [typeof(AccountJsonConverter).FullName!] = "Persists the template"
    };

    private static readonly Assembly[] ProductionAssemblies =
    [
        ProjectAssembly.Load("Financial.CashFlow.Domain"),
        ProjectAssembly.Load("Financial.CashFlow.Application"),
        ProjectAssembly.Load("Financial.CashFlow.Infrastructure"),
        ProjectAssembly.Load("Financial.Api"),
        ProjectAssembly.Load("Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport")
    ];

    private readonly IArchitectureQuery _query = new ArchUnitArchitectureQuery();

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-09")]
    public void Production_Should_Not_Read_The_Account_Template_Outside_The_Allow_List()
    {
        var readers = _query.TypesDependingOn(ProductionAssemblies, ReadsTheTemplate);

        readers.Should().BeEquivalentTo(AllowList.Keys);
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-09")]
    public void TemplateReadQuery_FindsPlantedViolation()
    {
        var readers = _query.TypesDependingOn([typeof(PlantedTemplateReader).Assembly], ReadsTheTemplate);

        readers.Should().Contain(typeof(PlantedTemplateReader).FullName);
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-09")]
    public void OnlyTheAdapter_ReferencesArchUnitNet()
    {
        var dependents = _query.TypesDependingOn(
            [typeof(AccountTemplateReadRuleTests).Assembly],
            name => name.Contains("ArchUnitNET.", StringComparison.Ordinal));

        dependents.Should().Equal(typeof(ArchUnitArchitectureQuery).FullName);
    }

    private static bool ReadsTheTemplate(string targetName) => targetName.Contains(TemplateGetter, StringComparison.Ordinal);
}
