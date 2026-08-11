using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CreditCardReferenceConverter(Dictionary<Guid, CreditCard>? lookup)
    : ReferenceConverter<CreditCard>(lookup, card => card.Id, "CreditCard");
