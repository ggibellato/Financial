using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;

namespace Financial.Investment.Infrastructure.Interfaces;

public interface IFinanceService
{
    AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request);
}
