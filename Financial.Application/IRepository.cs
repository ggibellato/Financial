﻿using Financial.Model;

namespace FinancialModel.Application;

public interface IRepository
{
    List<string> GetAllAssetsFullName();
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByPortifolio(string name);
    IEnumerable<Asset> GetAssetsByAssetName(string name);
}
