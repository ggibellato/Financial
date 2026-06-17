using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IInvestmentsSerializer
{
    string Serialize(Investments investments);
}
