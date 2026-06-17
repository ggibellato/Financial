using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Infrastructure.Persistence;

public sealed class InvestmentsSerializerAdapter : IInvestmentsSerializer
{
    public string Serialize(Investments investments) =>
        InvestmentsJsonSerializer.Serialize(investments);
}
