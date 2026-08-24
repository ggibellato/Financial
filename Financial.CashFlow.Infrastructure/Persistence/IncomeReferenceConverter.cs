using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class IncomeReferenceConverter(Dictionary<Guid, Income>? lookup)
    : ReferenceConverter<Income>(lookup, income => income.Id, "Income");
