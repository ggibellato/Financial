using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.TestUtilities;

public static class TestHoldingValuationService
{
    public static IHoldingValuationService Create(TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TestClock.At();
        return new HoldingValuationService(new XirrCalculationService(clock), new RecordingTelemetryTracer(), NullLogger<HoldingValuationService>.Instance, clock);
    }
}
