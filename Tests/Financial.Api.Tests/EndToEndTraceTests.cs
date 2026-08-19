using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

/// <summary>T041: one HTTP request must produce one correlated trace spanning
/// controller → ExpenseService → JsonStorage.Save. The controller root span comes from ASP.NET
/// Core's per-request Activity (auto-instrumentation); the two explicit spans are captured by a
/// RecordingTelemetryTracer registered via ConfigureTestServices, and correlation is proven by
/// both spans carrying that same request Activity's trace id.</summary>
public class EndToEndTraceTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    private const string CashFlowDataFileName = "data-cashflow.json";

    // The stock ApiTestFactory uses the LocalJson provider, whose bare LocalJsonStorage
    // deliberately produces no storage spans (T025/T026 wrap only the debounced/cloud path).
    // To exercise JsonStorage.Save the repository is rebuilt over a DebouncedJsonStorage
    // wrapping the same local temp file - the exact composition the GoogleDriveJson provider
    // uses in production, minus the Drive client.
    private static readonly TimeSpan TestDebounceWindow = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan SaveSpanTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SaveSpanPollInterval = TimeSpan.FromMilliseconds(25);

    [Fact]
    public async Task AddExpense_ProducesOneCorrelatedTrace_ThroughServiceAndStorageSave()
    {
        var tracer = new RecordingTelemetryTracer();
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITelemetryTracer>();
                services.AddSingleton<ITelemetryTracer>(tracer);

                services.RemoveAll<ICashFlowRepository>();
                services.AddSingleton<ICashFlowRepository>(sp =>
                {
                    var dataFilePath = sp.GetRequiredService<IConfiguration>()["CashFlow:DataJsonFile"];
                    var storage = new DebouncedJsonStorage(
                        new LocalJsonStorage(dataFilePath, CashFlowDataFileName),
                        TestDebounceWindow,
                        tracer: tracer);
                    var serializer = sp.GetRequiredService<ICashFlowSerializer>();
                    return new CashFlowJsonRepository(CashFlowLoader.LoadSync(storage, serializer), storage, serializer);
                });
            }));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The save is debounced onto a background task; wait for its span to be recorded.
        var saveSpan = await WaitForSpanAsync(tracer, "JsonStorage.Save");

        var serviceSpan = tracer.Spans.Should()
            .ContainSingle(s => s.Name == "CashFlow.ExpenseService.AddExpense").Which;
        serviceSpan.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        saveSpan.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);

        // Correlation: both spans were started under the same ambient Activity - ASP.NET Core's
        // per-request activity, the trace's root. The debounced save runs on a background task,
        // but Task.Run captures the request's ExecutionContext, so the trace id flows with it.
        serviceSpan.AmbientTraceId.Should().NotBeNull(
            "the ASP.NET Core request activity must be the ambient trace root");
        saveSpan.AmbientTraceId.Should().Be(serviceSpan.AmbientTraceId,
            "the storage save must belong to the same trace as the service call that caused it");
    }

    private static async Task<RecordingTelemetryTracer.RecordedSpan> WaitForSpanAsync(
        RecordingTelemetryTracer tracer, string spanName)
    {
        var deadline = DateTime.UtcNow + SaveSpanTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var span = tracer.Spans.FirstOrDefault(s => s.Name == spanName);
            if (span is not null)
            {
                return span;
            }

            await Task.Delay(SaveSpanPollInterval);
        }

        throw new TimeoutException($"Span '{spanName}' was not recorded within {SaveSpanTimeout.TotalSeconds}s.");
    }
}
