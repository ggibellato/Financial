using System.Diagnostics;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Investment.Infrastructure.Persistence;

public static class InvestmentLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    public static Investments LoadSync(IJsonStorage storage, IInvestmentSerializer serializer, TimeProvider timeProvider)
    {
        var json = storage.ReadAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        var investments = serializer.Deserialize(json);
        var createdAt = timeProvider.GetUtcNow();

        foreach (var failure in DisposalRecordBackfill.Apply(investments, createdAt))
        {
            Trace.TraceWarning($"DisposalRecord backfill skipped transaction {failure.TransactionId}: {failure.Message}");
        }

        foreach (var failure in TaxClassificationBackfill.Apply(investments, createdAt))
        {
            Trace.TraceWarning($"TaxClassification backfill skipped source {failure.SourceId}: {failure.Message}");
        }

        return investments;
    }
}
