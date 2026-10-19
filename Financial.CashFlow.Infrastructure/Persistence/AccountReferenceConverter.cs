using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class AccountReferenceConverter(Dictionary<Guid, Account>? lookup)
    : ReferenceConverter<Account>(lookup, account => account.Id);
