using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.TestUtilities;

public static class TestHoldingValuationService
{
    public static IHoldingValuationService Create() =>
        new HoldingValuationService(new XirrCalculationService(), new RecordingTelemetryTracer(), NullLogger<HoldingValuationService>.Instance);
}
