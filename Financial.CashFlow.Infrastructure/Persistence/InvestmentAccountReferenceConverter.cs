using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class InvestmentAccountReferenceConverter(Dictionary<Guid, InvestmentAccount>? lookup)
    : ReferenceConverter<InvestmentAccount>(lookup, account => account.Id, "InvestmentAccount");
