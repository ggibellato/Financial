using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public interface ICashFlowSerializer
{
    string Serialize(CashFlowData data);
    CashFlowData Deserialize(string json);
}
