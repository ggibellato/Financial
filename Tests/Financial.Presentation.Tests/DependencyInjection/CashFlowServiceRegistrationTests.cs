using System.IO;
using Financial.CashFlow.Application.DependencyInjection;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Presentation.Tests.DependencyInjection;

public class CashFlowServiceRegistrationTests
{
    [Fact]
    public void ResolvesAllTwelveCashFlowServices()
    {
        var provider = BuildServiceProvider();

        provider.GetRequiredService<IExpenseService>().Should().NotBeNull();
        provider.GetRequiredService<IIncomeService>().Should().NotBeNull();
        provider.GetRequiredService<IBankService>().Should().NotBeNull();
        provider.GetRequiredService<ITransferService>().Should().NotBeNull();
        provider.GetRequiredService<IBalanceAdjustmentService>().Should().NotBeNull();
        provider.GetRequiredService<ICardStatementService>().Should().NotBeNull();
        provider.GetRequiredService<IReserveService>().Should().NotBeNull();
        provider.GetRequiredService<IMensaisService>().Should().NotBeNull();
        provider.GetRequiredService<IControleMaeService>().Should().NotBeNull();
        provider.GetRequiredService<IInvestmentSnapshotService>().Should().NotBeNull();
        provider.GetRequiredService<IAnnualSummaryService>().Should().NotBeNull();
        provider.GetRequiredService<ITitheService>().Should().NotBeNull();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-presentation-di-{Guid.NewGuid()}.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CashFlow:Repository:Provider"] = "LocalJson",
                ["CashFlow:DataJsonFile"] = missingPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryTracer>(new RecordingTelemetryTracer());
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        services.AddFinancialCashFlowApplication();
        services.AddFinancialCashFlowInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
