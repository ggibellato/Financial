using System;

namespace Financial.Investment.Domain.Entities;

// SourceTransactionId is null only for AverageCost's synthetic entry.
public sealed record DisposalLotConsumption(Guid? SourceTransactionId, decimal Quantity, decimal UnitCost);
