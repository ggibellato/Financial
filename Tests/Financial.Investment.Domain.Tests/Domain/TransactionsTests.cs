using System.Collections;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionsTests
{
    /// <summary>Every test drives the same Transactions, so it is wired once here.</summary>
    private readonly Transactions _sut;

    public TransactionsTests()
    {
        _sut = new Transactions();
    }

    [Fact]
    public void Add_NullTransaction_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRange_AddsAllTransactionsAndRecalculates()
    {
        var items = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m),
            Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m),
        };

        _sut.AddRange(items);

        _sut.Quantity.Should().Be(20m);
        _sut.AveragePrice.Should().Be(6m);
        _sut.Should().HaveCount(2);
    }

    [Fact]
    public void Add_Buy_UpdatesAveragePriceAndQuantity()
    {
        var first = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var second = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);

        _sut.Add(first);
        _sut.Add(second);

        _sut.Quantity.Should().Be(20m);
        _sut.AveragePrice.Should().Be(6m);
    }

    [Fact]
    public void Add_Sell_DecreasesQuantityAndKeepsAveragePrice()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        _sut.Quantity.Should().Be(0m);
        _sut.AveragePrice.Should().Be(10m);
    }

    [Fact]
    public void Add_Sell_AccumulatesRealizedCapitalGainAtRunningCostBasis()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        // Weighted-average cost after both buys is 100; capital gain = 550 - (5 x 100) = 50
        _sut.RealizedCapitalGain.Should().Be(50m);
    }

    [Fact]
    public void AverageSellPrice_NoSales_IsNull()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.AverageSellPrice.Should().BeNull();
    }

    [Fact]
    public void AverageSellPrice_MultipleSales_IsWeightedAverage()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 6, 1), Transaction.TransactionType.Sell, 5m, 120m, 0m));

        // Weighted average = (5 x 110 + 5 x 120) / 10 = 115
        _sut.AverageSellPrice.Should().Be(115m);
    }

    [Fact]
    public void Update_RebuildsAndRecalculates()
    {
        var tx1Id = Guid.NewGuid();
        var tx1 = Transaction.CreateWithId(tx1Id, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var tx2 = Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);
        _sut.Add(tx1);
        _sut.Add(tx2);

        var updated = Transaction.CreateWithId(tx1Id, tx1.Date, tx1.Type, 20m, 5m, 0m);
        var result = _sut.Update(updated);

        result.Should().BeTrue();
        _sut.Quantity.Should().Be(30m);
        var expected = (20m * 5m + 10m * 7m) / 30m;
        _sut.AveragePrice.Should().Be(expected);
    }

    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        var result = _sut.Update(Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveById_UnknownId_ReturnsFalse()
    {
        _sut.RemoveById(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RemoveById_ExistingId_RemovesAndRecalculates()
    {
        var txId = Guid.NewGuid();
        _sut.Add(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var result = _sut.RemoveById(txId);

        result.Should().BeTrue();
        _sut.Should().BeEmpty();
        _sut.Quantity.Should().Be(0m);
    }

    /// <summary>
    /// Fees reduce what a sale returns, so they reduce the realized gain. Adding them to proceeds
    /// overstated this figure by twice the fee.
    /// </summary>
    [Fact]
    public void Add_SellWithFees_DeductsFeesFromRealizedCapitalGain()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 4m));

        _sut.RealizedCapitalGain.Should().Be(46m, "550 received less 4 in fees against a 500 cost basis");
    }

    [Fact]
    public void AverageSellPrice_WithFees_ReflectsNetProceedsPerUnit()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 5m));

        _sut.AverageSellPrice.Should().Be(109m, "545 net proceeds over 5 units");
    }

    [Fact]
    public void Add_BuyWithFees_KeepsFeesInTheCostBasis()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 20m));

        _sut.AveragePrice.Should().Be(102m, "a purchase cost basis still includes its fees");
    }

    [Fact]
    public void NonGenericGetEnumerator_EnumeratesTheSameItemsAsTheGenericOne()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        var items = new List<Transaction>();
        var enumerator = ((IEnumerable)_sut).GetEnumerator();
        while (enumerator.MoveNext())
        {
            items.Add((Transaction)enumerator.Current!);
        }

        items.Should().HaveCount(1);
    }

    [Fact]
    public void ExplicitClear_RemovesEveryTransaction()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        ((ICollection<Transaction>)_sut).Clear();

        _sut.Count.Should().Be(0);
        _sut.Quantity.Should().Be(0);
    }

    [Fact]
    public void ExplicitCopyTo_CopiesEveryTransactionIntoTheGivenArray()
    {
        var transaction = Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        _sut.Add(transaction);

        var target = new Transaction[1];
        ((ICollection<Transaction>)_sut).CopyTo(target, 0);

        target[0].Should().BeSameAs(transaction);
    }

    [Fact]
    public void Add_ShuffledOrder_ProducesSameFiguresAsChronologicalOrder()
    {
        var buy1 = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        var buy2 = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 120m, 0m);
        var sell1 = Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 8m, 130m, 0m);
        var buy3 = Transaction.Create(new DateTime(2024, 1, 15), Transaction.TransactionType.Buy, 3m, 90m, 0m);

        var chronological = new Transactions();
        chronological.AddRange([buy1, buy3, buy2, sell1]);

        var shuffled = new Transactions();
        shuffled.AddRange([sell1, buy2, buy1, buy3]);

        shuffled.Quantity.Should().Be(chronological.Quantity);
        shuffled.AveragePrice.Should().Be(chronological.AveragePrice);
        shuffled.RealizedCapitalGain.Should().Be(chronological.RealizedCapitalGain);
        shuffled.AverageSellPrice.Should().Be(chronological.AverageSellPrice);
    }

    [Fact]
    public void Add_SameDatePurchaseAndSale_AppliesPurchaseFirst()
    {
        var date = new DateTime(2024, 1, 1);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 100m, 10m, 0m);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 100m, 11m, 0m);

        _sut.Add(sell);
        _sut.Add(buy);

        _sut.Quantity.Should().Be(0m);
        _sut.RealizedCapitalGain.Should().Be(100m, "the sale is covered by the same-date purchase, at cost 10 x 100 = 1000 against proceeds 1100");
    }

    [Fact]
    public void Update_DateEditedBackwards_RederivesFiguresInNewOrder()
    {
        var buyId = Guid.NewGuid();
        var earlyBuy = Transaction.CreateWithId(buyId, new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        var laterBuy = Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 80m, 0m);
        _sut.Add(earlyBuy);
        _sut.Add(laterBuy);

        var backdated = Transaction.CreateWithId(buyId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        _sut.Update(backdated).Should().BeTrue();

        var expectedFromChronologicalEntry = new Transactions();
        expectedFromChronologicalEntry.AddRange([backdated, laterBuy]);

        _sut.AveragePrice.Should().Be(expectedFromChronologicalEntry.AveragePrice);
        _sut.Select(t => t.Id).Should().Equal([backdated.Id, laterBuy.Id]);
    }

    [Fact]
    public void Add_BuyAfterClosingToFlat_AveragePriceIgnoresThePriorClosedRun()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 60m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 5m, 200m, 0m));

        _sut.AveragePrice.Should().Be(200m, "the closed run's average (50) must not leak into the reopened position");
    }

    [Fact]
    public void Add_BuyResultingInZeroQuantityFromNegative_DoesNotThrowAndZeroesAveragePrice()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 12m, 0m));
        _sut.Quantity.Should().Be(-5m);

        Action act = () => _sut.Add(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 5m, 20m, 0m));

        act.Should().NotThrow();
        _sut.Quantity.Should().Be(0m);
        _sut.AveragePrice.Should().Be(0m);
    }

    [Fact]
    public void Add_BitcoinAndAgncSameDateShapes_ProduceExactlyTheSpecifiedFigures()
    {
        var bitcoin = new Transactions();
        bitcoin.AddRange(BitcoinStoredOrderTransactions());
        bitcoin.AveragePrice.Should().BeApproximately(62709.05039421211m, 0.00000001m);
        bitcoin.RealizedCapitalGain.Should().BeApproximately(0.1678589829253596m, 0.0000000000001m);

        var agnc = new Transactions();
        agnc.AddRange(AgncStoredOrderTransactions());
        agnc.RealizedCapitalGain.Should().BeApproximately(9.660847205052619m, 0.000000000001m);

        Math.Round(agnc.AveragePrice, 2).Should().Be(7.09m, "unchanged at the 2 dp both front ends display, even though full precision moves");
    }

    [Fact]
    public void Add_Redemption_BehavesLikeASaleForQuantityAndRealizedGain()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Redemption, 10m, 100m, 0m));

        _sut.Quantity.Should().Be(0m);
        _sut.RealizedCapitalGain.Should().Be(0m);
        _sut.AverageSellPrice.Should().Be(100m);
    }

    [Fact]
    public void Add_TransferIn_FeedsAveragePriceLikeABuyWithNoCashEffect()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.TransferIn, 10m, 120m, 0m));

        _sut.Quantity.Should().Be(20m);
        _sut.AveragePrice.Should().Be(110m, "TransferIn contributes its own recorded cost basis to the weighted average, same as a Buy");
    }

    [Fact]
    public void Add_TransferOut_ReducesQuantityAtExistingAveragePriceWithZeroRealizedGain()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.TransferOut, 4m, 999m, 0m));

        _sut.Quantity.Should().Be(6m);
        _sut.AveragePrice.Should().Be(100m, "a TransferOut never changes the average price of what remains");
        _sut.RealizedCapitalGain.Should().Be(0m, "no consideration changed hands, so no gain or loss is realized");
        _sut.AverageSellPrice.Should().BeNull("a TransferOut is not a sale for this metric's purpose");
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital)]
    public void Add_NoQuantityEffectType_LeavesQuantityAveragePriceAndRealizedGainUnchanged(Transaction.TransactionType type)
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 2, 1), type, 0m, 0m, fees: 5m));

        _sut.Quantity.Should().Be(10m);
        _sut.AveragePrice.Should().Be(100m);
        _sut.RealizedCapitalGain.Should().Be(0m);
    }

    private static IEnumerable<Transaction> BitcoinStoredOrderTransactions() =>
    [
        Transaction.Create(new DateTime(2025, 3, 7), Transaction.TransactionType.Buy, 0.00141516m, 67949.91m, 3.84m),
        Transaction.Create(new DateTime(2025, 3, 21), Transaction.TransactionType.Buy, 0.00145858m, 65927.13m, 3.84m),
        Transaction.Create(new DateTime(2025, 3, 27), Transaction.TransactionType.Buy, 0.00070682m, 67923.94m, 1.99m),
        Transaction.Create(new DateTime(2025, 4, 26), Transaction.TransactionType.Buy, 0.00134701m, 71387.74m, 3.84m),
        Transaction.Create(new DateTime(2025, 5, 8), Transaction.TransactionType.Buy, 0.00067751m, 77223.95m, 2.99m),
        Transaction.Create(new DateTime(2025, 8, 12), Transaction.TransactionType.Buy, 0.00053698m, 89407.43m, 1.99m),
        Transaction.Create(new DateTime(2025, 11, 7), Transaction.TransactionType.Sell, 0.00013155m, 76016.72m, 0m),
        Transaction.Create(new DateTime(2025, 11, 7), Transaction.TransactionType.Buy, 0.00061878m, 77588.16m, 1.99m),
        Transaction.Create(new DateTime(2025, 11, 21), Transaction.TransactionType.Buy, 0.00206477m, 63367.83m, 5.22m),
        Transaction.Create(new DateTime(2025, 12, 5), Transaction.TransactionType.Buy, 0.00137693m, 69836.52m, 3.84m),
        Transaction.Create(new DateTime(2025, 12, 16), Transaction.TransactionType.Buy, 0.00073298m, 65499.74m, 1.99m),
        Transaction.Create(new DateTime(2026, 2, 6), Transaction.TransactionType.Buy, 0.00201065m, 48243.11m, 2.99m),
        Transaction.Create(new DateTime(2026, 2, 16), Transaction.TransactionType.Buy, 0.00193999m, 50000.26m, 2.99m),
        Transaction.Create(new DateTime(2026, 2, 16), Transaction.TransactionType.Buy, 0.00197649m, 49076.90m, 2.99m),
        Transaction.Create(new DateTime(2026, 3, 14), Transaction.TransactionType.Buy, 0.00088773m, 54070.49m, 1.99m),
        Transaction.Create(new DateTime(2026, 6, 9), Transaction.TransactionType.Buy, 0.00092819m, 46337.50m, 1.99m),
        Transaction.Create(new DateTime(2026, 7, 31), Transaction.TransactionType.Buy, 0.00104326m, 47926.69m, 1.99m),
    ];

    private static IEnumerable<Transaction> AgncStoredOrderTransactions() =>
    [
        Transaction.Create(new DateTime(2025, 4, 7), Transaction.TransactionType.Buy, 132m, 6.645681835587176m, 1.320000000000068m),
        Transaction.Create(new DateTime(2025, 4, 7), Transaction.TransactionType.Buy, 14.9976m, 6.6417293950659735m, 0.15000000000000582m),
        Transaction.Create(new DateTime(2025, 4, 9), Transaction.TransactionType.Buy, 1.702042m, 6.498077042280516m, 0.020000000000000986m),
        Transaction.Create(new DateTime(2025, 4, 9), Transaction.TransactionType.Buy, 0.08172m, 6.485560473238939m, 0.0000000000000001049200m),
        Transaction.Create(new DateTime(2025, 5, 9), Transaction.TransactionType.Buy, 1.7031m, 6.699547887289302m, 0.019999999999999764m),
        Transaction.Create(new DateTime(2025, 6, 10), Transaction.TransactionType.Buy, 1.65524m, 6.85097028178425m, 0.02000000000000103m),
        Transaction.Create(new DateTime(2025, 7, 10), Transaction.TransactionType.Buy, 1.47466563m, 7.01175902249348m, 0.019999999999999143m),
        Transaction.Create(new DateTime(2025, 7, 10), Transaction.TransactionType.Buy, 0.14833574m, 7.01112221549868m, 0.0000000000000000331768m),
        Transaction.Create(new DateTime(2025, 8, 11), Transaction.TransactionType.Buy, 1.64388103m, 7.068638092202437m, 0.019999999999999896m),
        Transaction.Create(new DateTime(2025, 9, 10), Transaction.TransactionType.Buy, 1.52379543m, 7.671633472183008m, 0.020000000000000285m),
        Transaction.Create(new DateTime(2025, 10, 9), Transaction.TransactionType.Buy, 1.56854597m, 7.618520763037246m, 0.019999999999999827m),
        Transaction.Create(new DateTime(2025, 11, 12), Transaction.TransactionType.Buy, 1.55456011m, 7.89934070903026m, 0.02000000000000002m),
        Transaction.Create(new DateTime(2025, 12, 9), Transaction.TransactionType.Buy, 1.55190297m, 7.880647355175415m, 0.019999999999998592m),
        Transaction.Create(new DateTime(2026, 1, 12), Transaction.TransactionType.Buy, 1.21772313m, 8.581589491813205m, 0.019999999999998633m),
        Transaction.Create(new DateTime(2026, 1, 12), Transaction.TransactionType.Buy, 0.21278086m, 8.459407532953124m, 0m),
        Transaction.Create(new DateTime(2026, 2, 10), Transaction.TransactionType.Buy, 1.4608111m, 8.310451658515305m, 0.019999999999999934m),
        Transaction.Create(new DateTime(2026, 3, 10), Transaction.TransactionType.Buy, 1.5528305m, 8.011177063444189m, 0.019999999999998273m),
        Transaction.Create(new DateTime(2026, 4, 10), Transaction.TransactionType.Buy, 1.62122007m, 7.771924537822558m, 0.019999999999999872m),
        Transaction.Create(new DateTime(2026, 5, 11), Transaction.TransactionType.Buy, 1.58162836m, 7.947505465358008m, 0.019999999999999993m),
        Transaction.Create(new DateTime(2026, 6, 8), Transaction.TransactionType.Buy, 18.9875759m, 7.640785838284647m, 0.2199999999999893m),
        Transaction.Create(new DateTime(2026, 6, 9), Transaction.TransactionType.Buy, 1.68002128m, 7.678474212404013m, 0.0200000000000002m),
        Transaction.Create(new DateTime(2026, 6, 12), Transaction.TransactionType.Buy, 23.86404m, 7.726269315673289m, 0.28000000000000436m),
        Transaction.Create(new DateTime(2026, 7, 10), Transaction.TransactionType.Buy, 1.95241333m, 8.307667128840933m, 0.01999999999999796m),
        Transaction.Create(new DateTime(2026, 7, 17), Transaction.TransactionType.Buy, 7.29407543m, 8.463032910278699m, 0.09000000000000218m),
        Transaction.Create(new DateTime(2026, 7, 17), Transaction.TransactionType.Sell, 7.29407543m, 8.394484126495225m, 0m),
        Transaction.Create(new DateTime(2026, 7, 17), Transaction.TransactionType.Buy, 7.26068567m, 8.408296843419054m, 0.09000000000000082m),
        Transaction.Create(new DateTime(2026, 7, 31), Transaction.TransactionType.Buy, 1.52146573m, 8.018583517742762m, 0.020000000000000663m),
        Transaction.Create(new DateTime(2026, 8, 11), Transaction.TransactionType.Buy, 2.10600557m, 7.97718693413219m, 0.030000000000004745m),
    ];
}
