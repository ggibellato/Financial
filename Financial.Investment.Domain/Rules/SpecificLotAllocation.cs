using System;

namespace Financial.Investment.Domain.Rules;

/// <summary>The caller's chosen lot/quantity pair for a SpecificId sale. Distinct from
/// <see cref="Entities.DisposalLotConsumption"/>, which additionally carries the lot's resolved
/// UnitCost once <see cref="DisposalRecordCalculator"/> has matched it against an open lot.</summary>
public sealed record SpecificLotAllocation(Guid SourceTransactionId, decimal Quantity);
