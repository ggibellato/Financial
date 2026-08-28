using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Infrastructure.Persistence;

public interface IInvestmentSerializer
{
    string Serialize(Investments investments);
    Investments Deserialize(string json);
}
