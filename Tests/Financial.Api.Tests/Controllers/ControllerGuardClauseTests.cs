using Financial.Api.Controllers;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Financial.Api.Tests.Controllers;

// These guard clauses (constructor null-checks and non-nullable [FromBody] null-checks) are
// unreachable via real HTTP calls: DI never passes null constructor args, and [ApiController]'s
// automatic model validation short-circuits a null body for non-nullable [FromBody] parameters
// before the action method ever runs. They're tested by calling the controllers directly.
public class ControllerGuardClauseTests
{
    [Fact]
    public void AssetPricesController_NullService_Throws()
    {
        Action act = () => new AssetPricesController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AssetsController_NullNavigationService_Throws()
    {
        Action act = () => new AssetsController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NavigationController_NullNavigationService_Throws()
    {
        Action act = () => new NavigationController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void XirrController_NullXirrCalculationService_Throws()
    {
        Action act = () => new XirrController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SummaryController_NullSummaryService_Throws()
    {
        Action act = () => new SummaryController(null!, new StubPortfolioAssetSummaryService(), new StubBrokerBreakdownService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("summaryService");
    }

    [Fact]
    public void SummaryController_NullPortfolioAssetSummaryService_Throws()
    {
        Action act = () => new SummaryController(new StubSummaryService(), null!, new StubBrokerBreakdownService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("portfolioAssetSummaryService");
    }

    [Fact]
    public void SummaryController_NullBrokerBreakdownService_Throws()
    {
        Action act = () => new SummaryController(new StubSummaryService(), new StubPortfolioAssetSummaryService(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("brokerBreakdownService");
    }

    [Fact]
    public void CreditsController_NullCreditQueryService_Throws()
    {
        Action act = () => new CreditsController(null!, new StubCreditService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditQueryService");
    }

    [Fact]
    public void CreditsController_NullCreditService_Throws()
    {
        Action act = () => new CreditsController(new StubCreditQueryService(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditService");
    }

    [Fact]
    public async Task CreditsController_AddCredit_NullRequest_ReturnsBadRequest()
    {
        var controller = new CreditsController(new StubCreditQueryService(), new StubCreditService());

        var result = await controller.AddCredit(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public async Task CreditsController_UpdateCredit_NullRequest_ReturnsBadRequest()
    {
        var controller = new CreditsController(new StubCreditQueryService(), new StubCreditService());

        var result = await controller.UpdateCredit(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public async Task CreditsController_DeleteCredit_NullRequest_ReturnsBadRequest()
    {
        var controller = new CreditsController(new StubCreditQueryService(), new StubCreditService());

        var result = await controller.DeleteCredit(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void TransactionsController_NullTransactionService_Throws()
    {
        Action act = () => new TransactionsController(null!, new StubTransactionQueryService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionService");
    }

    [Fact]
    public void TransactionsController_NullTransactionQueryService_Throws()
    {
        Action act = () => new TransactionsController(new StubTransactionService(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transactionQueryService");
    }

    [Fact]
    public async Task TransactionsController_AddTransaction_NullRequest_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = await controller.AddTransaction(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public async Task TransactionsController_UpdateTransaction_NullRequest_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = await controller.UpdateTransaction(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public async Task TransactionsController_DeleteTransaction_NullRequest_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = await controller.DeleteTransaction(null!);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void DividendsController_NullDividendService_Throws()
    {
        Action act = () => new DividendsController(null!, Options.Create(new DividendOptions()));
        act.Should().Throw<ArgumentNullException>().WithParameterName("dividendService");
    }

    [Fact]
    public void DividendsController_NullDividendOptions_Throws()
    {
        Action act = () => new DividendsController(new StubDividendService(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dividendOptions");
    }

    [Fact]
    public void DiagnosticsController_NullRepositorySettings_Throws()
    {
        Action act = () => new DiagnosticsController(null!, new StubHostEnvironment());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DiagnosticsController_NullEnvironment_Throws()
    {
        Action act = () => new DiagnosticsController(Options.Create(new RepositorySettingsOptions()), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("environment");
    }

    [Fact]
    public void AnnualSummaryController_NullService_Throws()
    {
        Action act = () => new AnnualSummaryController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BanksController_NullService_Throws()
    {
        Action act = () => new BanksController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CardStatementsController_NullService_Throws()
    {
        Action act = () => new CardStatementsController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ControleMaeController_NullService_Throws()
    {
        Action act = () => new ControleMaeController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExpensesController_NullService_Throws()
    {
        Action act = () => new ExpensesController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IncomesController_NullService_Throws()
    {
        Action act = () => new IncomesController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InvestmentSnapshotsController_NullService_Throws()
    {
        Action act = () => new InvestmentSnapshotsController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MensaisController_NullService_Throws()
    {
        Action act = () => new MensaisController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReserveController_NullService_Throws()
    {
        Action act = () => new ReserveController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TitheController_NullService_Throws()
    {
        Action act = () => new TitheController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Financial.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
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

    private sealed class StubBrokerBreakdownService : IBrokerBreakdownService
    {
        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubCreditQueryService : ICreditQueryService
    {
        public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubCreditService : ICreditService
    {
        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request) => throw new NotImplementedException();
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request) => throw new NotImplementedException();
    }

    private sealed class StubTransactionQueryService : ITransactionQueryService
    {
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }

    private sealed class StubDividendService : IDividendService
    {
        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request) => throw new NotImplementedException();
        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request) => throw new NotImplementedException();
    }
}
