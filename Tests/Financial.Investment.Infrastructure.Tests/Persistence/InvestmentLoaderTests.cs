using System;
using System.IO;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Persistence;

public class InvestmentLoaderTests
{
    [Fact]
    public void LoadSync_LegacyDocumentWithSellAndNoDisposalRecords_BackfillsActiveRecordOnLoad()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var serializer = new InvestmentSerializerAdapter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, serializer.Serialize(investments));

        try
        {
            var storage = new LocalJsonStorage(tempFile);

            var loaded = InvestmentLoader.LoadSync(storage, serializer);

            var loadedAsset = loaded.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
            loadedAsset.DisposalRecords.Should().ContainSingle();
            loadedAsset.DisposalRecords.Single().Status.Should().Be(DisposalRecordStatus.Active);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadSync_CalledOnAFileAlreadyBackfilled_DoesNotDuplicateRecords()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var serializer = new InvestmentSerializerAdapter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, serializer.Serialize(investments));

        try
        {
            var storage = new LocalJsonStorage(tempFile);

            var loaded = InvestmentLoader.LoadSync(storage, serializer);

            var loadedAsset = loaded.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
            loadedAsset.DisposalRecords.Should().ContainSingle();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
