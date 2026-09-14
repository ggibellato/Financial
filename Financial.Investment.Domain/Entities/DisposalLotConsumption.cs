using System;

namespace Financial.Investment.Domain.Entities;

/// <summary>A single lot's contribution to a DisposalRecord's cost basis. SourceTransactionId is
/// null only for AverageCost's synthetic entry, which carries the blended average price instead of
/// a specific purchase lot.</summary>
public sealed record DisposalLotConsumption(Guid? SourceTransactionId, decimal Quantity, decimal UnitCost);
