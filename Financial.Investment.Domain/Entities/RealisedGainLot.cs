using System;

namespace Financial.Investment.Domain.Entities;

// SourceTransactionId is null only for AverageCost's synthetic entry.
public sealed record RealisedGainLot(Guid? SourceTransactionId, decimal Quantity, decimal UnitCost);
