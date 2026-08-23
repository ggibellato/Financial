using Financial.Investment.Domain.Entities;
using Financial.Integrations.GoogleSheets;
using Financial.Integrations.GoogleSheets.DTO;
using Financial.Investment.Infrastructure.SpreadsheetImport;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.SpreadsheetImport;

public class GoogleSheetsAssetReaderTests
{
    /// <summary>Every test reads through the same stub data source; each one just seeds different rows.</summary>
    private readonly StubDataSource _dataSource;
    private readonly GoogleSheetsAssetReader _sut;

    public GoogleSheetsAssetReaderTests()
    {
        _dataSource = new StubDataSource();
        _sut = new GoogleSheetsAssetReader(_dataSource);
    }

    private sealed class StubDataSource : IGoogleSheetsDataSource
    {
        public IList<IList<object>>? Rows { get; set; }
        public string? LastSpreadSheetId { get; private set; }
        public string? LastRange { get; private set; }

        public Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId) =>
            Task.FromResult(new List<SheetDTO>());

        public Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range)
        {
            LastSpreadSheetId = spreadSheetId;
            LastRange = range;
            return Task.FromResult<IList<IList<object>>>(Rows!);
        }
    }

    [Fact]
    public async Task GetAssetDataAsync_WithValidRow_ReturnsExchangeIdTickerAndIsin()
    {
        _dataSource.Rows = new List<IList<object>> { new List<object> { "BVMF", "PETR4", "BRPETRACNPR6" } };

        var result = await _sut.GetAssetDataAsync("file1", "Sheet1");

        result.exchangeId.Should().Be("BVMF");
        result.ticker.Should().Be("PETR4");
        result.isin.Should().Be("BRPETRACNPR6");
    }

    [Fact]
    public async Task GetAssetDataAsync_NullResponse_ReturnsEmptyStrings()
    {
        _dataSource.Rows = null;

        var result = await _sut.GetAssetDataAsync("file1", "Sheet1");

        result.exchangeId.Should().BeEmpty();
        result.ticker.Should().BeEmpty();
        result.isin.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssetDataAsync_RowShorterThanExpectedColumns_ReturnsFieldsExtractedBeforeTheOutOfRangeAccess()
    {
        // ArgumentOutOfRangeException is caught, but fields are assigned sequentially before it's
        // thrown, so exchangeId (column 0) is already set by the time ticker (column 1) fails.
        _dataSource.Rows = new List<IList<object>> { new List<object> { "BVMF" } };

        var result = await _sut.GetAssetDataAsync("file1", "Sheet1");

        result.exchangeId.Should().Be("BVMF");
        result.ticker.Should().BeEmpty();
        result.isin.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssetDataAsync_RequestsTheQ2S2Range()
    {
        _dataSource.Rows = null;

        await _sut.GetAssetDataAsync("file1", "Sheet1");

        _dataSource.LastSpreadSheetId.Should().Be("file1");
        _dataSource.LastRange.Should().Be("Sheet1!Q2:S2");
    }

    [Fact]
    public async Task ReadTransactionsAsync_SellCode_MapsToSellTransactionType()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "V", "10", "", "100", "0" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Type.Should().Be(Transaction.TransactionType.Sell);
    }

    [Fact]
    public async Task ReadTransactionsAsync_NonSellCode_MapsToBuyTransactionType()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "C", "10", "", "100", "0" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Type.Should().Be(Transaction.TransactionType.Buy);
    }

    /// <summary>
    /// A recorded total that disagrees with quantity x unit price derives a negative fee, which
    /// Transaction floors to zero on construction. The floor is deliberate - a negative fee would propagate
    /// into Realized Gain/Loss - but it is a repair, and a silent repair made a spreadsheet with
    /// bad totals import looking exactly like a clean one.
    /// </summary>
    [Fact]
    public async Task ReadTransactionsAsync_WhenTheDerivedFeeIsNegative_ReportsTheRowToTheImportLog()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "C", "10", "", "100", "900" }
        };
        var importLog = new RecordingProgress();

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1", importLog);

        result.Should().ContainSingle().Which.Fees.Should().Be(0m, "the floor still applies");
        var reported = importLog.Messages.Should().ContainSingle().Subject;
        reported.Should().Contain("Sheet1").And.Contain("900").And.Contain("-100");
    }

    [Fact]
    public async Task ReadTransactionsAsync_WhenTheDerivedFeeIsPositive_ReportsNothing()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "C", "10", "", "100", "1010" }
        };
        var importLog = new RecordingProgress();

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1", importLog);

        result.Should().ContainSingle().Which.Fees.Should().Be(10m);
        importLog.Messages.Should().BeEmpty();
    }

    /// <summary>The tool passes a progress sink, but the reader must not require one.</summary>
    [Fact]
    public async Task ReadTransactionsAsync_WithNoImportLog_StillFloorsTheNegativeFee()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "C", "10", "", "100", "900" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Fees.Should().Be(0m);
    }

    private sealed class RecordingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = new();

        public void Report(string value) => Messages.Add(value);
    }

    [Fact]
    public async Task ReadTransactionsAsync_MissingDateOnSubsequentRow_ReusesPreviousDate()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "", "C", "10", "", "100", "0" },
            new List<object> { "", "", "C", "5", "", "100", "0" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().HaveCount(2);
        result[1].Date.Should().Be(result[0].Date);
    }

    [Fact]
    public async Task ReadTransactionsAsync_ComputesFeesAsTotalPriceMinusUnitPriceTimesQuantity()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            // Total price cell (column 6) = 1005, unitPrice*quantity = 100*10 = 1000, so fees = 5.
            new List<object> { 45000L, "", "C", "10", "", "100", "1005" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Fees.Should().Be(5m);
    }

    [Fact]
    public async Task ReadTransactionsAsync_NegativeComputedFees_ClampsToZero()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            // Total price cell (column 6) = 500, unitPrice*quantity = 100*10 = 1000, so raw fees = -500.
            new List<object> { 45000L, "", "C", "10", "", "100", "500" }
        };

        var result = await _sut.ReadTransactionsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Fees.Should().Be(0m);
    }

    [Fact]
    public async Task ReadCreditsAsync_RentType_MapsToRentCreditType()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "100", "", "Aluguel" }
        };

        var result = await _sut.ReadCreditsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Type.Should().Be(Credit.CreditType.Rent);
    }

    [Fact]
    public async Task ReadCreditsAsync_NonRentType_MapsToDividendCreditType()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { 45000L, "100", "", "Dividendo" }
        };

        var result = await _sut.ReadCreditsAsync("file1", "Sheet1");

        result.Should().ContainSingle().Which.Type.Should().Be(Credit.CreditType.Dividend);
    }

    [Fact]
    public async Task ReadCreditsAsync_RowWithBlankDate_IsSkipped()
    {
        _dataSource.Rows = new List<IList<object>>
        {
            new List<object> { "", "100", "", "Dividendo" }
        };

        var result = await _sut.ReadCreditsAsync("file1", "Sheet1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadCreditsAsync_NullResponse_ReturnsEmptyList()
    {
        _dataSource.Rows = null;

        var result = await _sut.ReadCreditsAsync("file1", "Sheet1");

        result.Should().BeEmpty();
    }
}
