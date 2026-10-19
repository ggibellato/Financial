using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class IncomeReferenceConverter(Dictionary<Guid, Income>? lookup)
    : ReferenceConverter<Income>(lookup, income => income.Id);
