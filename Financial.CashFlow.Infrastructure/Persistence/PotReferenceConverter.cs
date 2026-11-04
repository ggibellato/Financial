using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class PotReferenceConverter(Dictionary<Guid, Pot>? lookup)
    : ReferenceConverter<Pot>(lookup, pot => pot.Id);
