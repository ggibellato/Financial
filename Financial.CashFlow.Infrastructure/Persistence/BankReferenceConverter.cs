using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class BankReferenceConverter(Dictionary<Guid, Bank>? lookup)
    : ReferenceConverter<Bank>(lookup, bank => bank.Id, "Bank");
