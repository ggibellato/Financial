using System;

namespace Financial.Investment.Domain.Rules;

// Caller's requested allocation, before UnitCost is resolved — not to be confused with RealisedGainLot.
public sealed record SpecificLotAllocation(Guid SourceTransactionId, decimal Quantity);
