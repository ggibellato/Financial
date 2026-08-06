using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class IncomeSourceReferenceConverter(Dictionary<Guid, IncomeSource>? lookup)
    : ReferenceConverter<IncomeSource>(lookup, source => source.Id, "IncomeSource");
