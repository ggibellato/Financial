using Financial.Api.Controllers;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Financial.Api.Tests.Controllers;

/// <summary>The dividend lookup catches swallow provider exceptions into a 404; the failure
/// must be visible in the log stream (logging-audit.md residual finding), carrying the public
/// ticker symbol and the exception type - never the provider's message.</summary>
public class DividendsControllerLoggingTests
{
    private const string ProviderMessage = "provider row said: account 12345 balance 999.99";

    [Fact]
    public void GetDividendHistory_ServiceThrows_Returns404AndLogsTickerAndErrorTypeOnly()
    {
        var logger = new RecordingLogger<DividendsController>();
        var controller = CreateController(logger);

        var result = controller.GetDividendHistory("VWRL");

        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var entry = logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning).Which;
        entry.Message.Should().Contain("VWRL");
        entry.Message.Should().Contain(nameof(InvalidOperationException));
        entry.Message.Should().NotContain("999.99", "provider exception messages are never logged");
    }

    [Fact]
    public void GetDividendSummary_ServiceThrows_Returns404AndLogsTickerAndErrorTypeOnly()
    {
        var logger = new RecordingLogger<DividendsController>();
        var controller = CreateController(logger);

        var result = controller.GetDividendSummary("VWRL");

        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var entry = logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning).Which;
        entry.Message.Should().Contain("VWRL");
        entry.Message.Should().Contain(nameof(InvalidOperationException));
        entry.Message.Should().NotContain("999.99", "provider exception messages are never logged");
    }

    private static DividendsController CreateController(RecordingLogger<DividendsController> logger) =>
        new(new ThrowingDividendService(), Options.Create(new DividendOptions()), logger);

    private sealed class ThrowingDividendService : IDividendService
    {
        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request) =>
            throw new InvalidOperationException(ProviderMessage);

        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request) =>
            throw new InvalidOperationException(ProviderMessage);
    }
}
