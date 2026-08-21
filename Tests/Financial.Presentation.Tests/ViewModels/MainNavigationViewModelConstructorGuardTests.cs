using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class MainNavigationViewModelConstructorGuardTests
{
    private static readonly INavigationService ValidNavigationService = new StubNavigationService();
    private static readonly ICreditQueryService ValidCreditQueryService = new StubCreditQueryService();
    private static readonly ISummaryService ValidSummaryService = new StubSummaryService();
    private static readonly IPortfolioAssetSummaryService ValidPortfolioAssetSummaryService = new StubPortfolioAssetSummaryService();
    private static readonly ITransactionService ValidTransactionService = new StubTransactionService();
    private static readonly ICreditService ValidCreditService = new StubCreditService();
    private static readonly IAssetPriceService ValidAssetPriceService = new StubAssetPriceService();
    private static readonly IBrokerBreakdownService ValidBrokerBreakdownService = new StubBrokerBreakdownService();
    private static readonly ITransactionQueryService ValidTransactionQueryService = new StubTransactionQueryService();
    private static readonly IXirrCalculationService ValidXirrCalculationService = new StubXirrCalculationService();
    private static readonly IProfitCalculationService ValidProfitCalculationService = new StubProfitCalculationService();
    private static readonly IPriceService ValidPriceService = new StubPriceService();
    private static readonly IAssetMoveService ValidAssetMoveService = new StubAssetMoveService();

    [Fact]
    public void MainNavigationViewModel_NullNavigationService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            null!, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void MainNavigationViewModel_NullCreditQueryService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, null!, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditQueryService");
    }

    [Fact]
    public void MainNavigationViewModel_NullSummaryService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, null!, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("summaryService");
    }

    [Fact]
    public void MainNavigationViewModel_NullPortfolioAssetSummaryService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, null!,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("portfolioAssetSummaryService");
    }

    [Fact]
    public void MainNavigationViewModel_NullTransactionService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            null!, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionService");
    }

    [Fact]
    public void MainNavigationViewModel_NullCreditService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, null!, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditService");
    }

    [Fact]
    public void MainNavigationViewModel_NullAssetPriceService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, null!, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("assetPriceService");
    }

    [Fact]
    public void MainNavigationViewModel_NullBrokerBreakdownService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, null!,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("brokerBreakdownService");
    }

    [Fact]
    public void MainNavigationViewModel_NullTransactionQueryService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            null!, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionQueryService");
    }

    [Fact]
    public void MainNavigationViewModel_NullXirrCalculationService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, null!, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("xirrCalculationService");
    }

    [Fact]
    public void MainNavigationViewModel_NullPriceService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, null!, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("priceService");
    }

    [Fact]
    public void MainNavigationViewModel_NullProfitCalculationService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, null!, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("profitCalculationService");
    }

    [Fact]
    public void MainNavigationViewModel_NullAssetMoveService_Throws()
    {
        Action act = () => new MainNavigationViewModel(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("assetMoveService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullNavigationService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            null!, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullCreditQueryService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, null!, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditQueryService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullSummaryService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, null!, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("summaryService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullPortfolioAssetSummaryService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, null!,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("portfolioAssetSummaryService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullTransactionService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            null!, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullCreditService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, null!, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullAssetPriceService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, null!, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("assetPriceService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullBrokerBreakdownService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, null!,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("brokerBreakdownService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullTransactionQueryService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            null!, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionQueryService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullXirrCalculationService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, null!, ValidProfitCalculationService, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("xirrCalculationService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullPriceService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, null!, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("priceService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullProfitCalculationService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, null!, ValidPriceService, ValidAssetMoveService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("profitCalculationService");
    }

    [Fact]
    public void MainNavigationViewModelHistoric_NullAssetMoveService_Throws()
    {
        Action act = () => new MainNavigationViewModelHistoric(
            ValidNavigationService, ValidCreditQueryService, ValidSummaryService, ValidPortfolioAssetSummaryService,
            ValidTransactionService, ValidCreditService, ValidAssetPriceService, ValidBrokerBreakdownService,
            ValidTransactionQueryService, ValidXirrCalculationService, ValidProfitCalculationService, ValidPriceService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("assetMoveService");
    }

    private sealed class StubNavigationService : INavigationService
    {
        public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName) => throw new NotImplementedException();
    }

    private sealed class StubCreditQueryService : ICreditQueryService
    {
        public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubSummaryService : ISummaryService
    {
        public AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubPortfolioAssetSummaryService : IPortfolioAssetSummaryService
    {
        public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request) => throw new NotImplementedException();
    }

    private sealed class StubCreditService : ICreditService
    {
        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request) => throw new NotImplementedException();
    }

    private sealed class StubAssetPriceService : IAssetPriceService
    {
        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) => throw new NotImplementedException();
    }

    private sealed class StubBrokerBreakdownService : IBrokerBreakdownService
    {
        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubTransactionQueryService : ITransactionQueryService
    {
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubXirrCalculationService : IXirrCalculationService
    {
        public decimal? Calculate(IReadOnlyList<AssetCashFlowDTO> cashFlows, decimal terminalValue) => throw new NotImplementedException();
    }

    private sealed class StubProfitCalculationService : IProfitCalculationService
    {
        public bool HasCostBasis(decimal averagePrice, decimal quantity) => throw new NotImplementedException();
        public decimal CalculateResultFraction(decimal averagePrice, decimal quantity, decimal currentValue) => throw new NotImplementedException();
        public decimal? CalculateProfitPercent(decimal currentValue, decimal costBasis) => throw new NotImplementedException();
    }

    private sealed class StubPriceService : IPriceService
    {
        public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request) => throw new NotImplementedException();
        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request) => throw new NotImplementedException();
    }

    private sealed class StubAssetMoveService : IAssetMoveService
    {
        public Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO> ArchiveAssetAsync(ArchiveAssetRequestDTO request) => throw new NotImplementedException();
    }
}
