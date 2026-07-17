using Financial.Domain.Entities;

namespace Financial.Infrastructure.Persistence;

public interface IInvestmentsSerializer
{
    string Serialize(Investments investments);
    Investments Deserialize(string json);
}
