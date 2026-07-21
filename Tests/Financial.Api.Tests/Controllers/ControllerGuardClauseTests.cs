using Financial.Api.Controllers;
using Financial.Application.Configuration;
using Financial.Application.DTOs;
using Financial.Application.Enums;
using Financial.Application.Interfaces;
using FluentAssertions;
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

    // The following whitespace-route-parameter guards are unreachable via real HTTP: [ApiController]'s
    // automatic model validation treats a whitespace-only bound route string as "required field missing"
    // and returns its own ProblemDetails 400 before the action method runs, so these guards never fire
    // in production traffic but are tested directly here for defense-in-depth coverage.
    [Fact]
    public void SummaryController_GetPortfolioAssetsSummary_WhitespaceBrokerName_ReturnsBadRequest()
    {
        var controller = new SummaryController(new StubSummaryService(), new StubPortfolioAssetSummaryService(), new StubBrokerBreakdownService());

        var result = controller.GetPortfolioAssetsSummary(" ", "Default", null);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void SummaryController_GetPortfolioAssetsSummary_WhitespacePortfolioName_ReturnsBadRequest()
    {
        var controller = new SummaryController(new StubSummaryService(), new StubPortfolioAssetSummaryService(), new StubBrokerBreakdownService());

        var result = controller.GetPortfolioAssetsSummary("XPI", " ", null);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void SummaryController_GetBrokerBreakdown_WhitespaceBrokerName_ReturnsBadRequest()
    {
        var controller = new SummaryController(new StubSummaryService(), new StubPortfolioAssetSummaryService(), new StubBrokerBreakdownService());

        var result = controller.GetBrokerBreakdown(" ", null);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void DividendsController_GetDividendHistory_WhitespaceTicker_ReturnsBadRequest()
    {
        var controller = new DividendsController(new StubDividendService(), Options.Create(new DividendOptions()));

        var result = controller.GetDividendHistory(" ");

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void DividendsController_GetDividendSummary_WhitespaceTicker_ReturnsBadRequest()
    {
        var controller = new DividendsController(new StubDividendService(), Options.Create(new DividendOptions()));

        var result = controller.GetDividendSummary(" ");

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void TransactionsController_GetTransactionsByBroker_WhitespaceBrokerName_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = controller.GetTransactionsByBroker(" ");

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void TransactionsController_GetTransactionsByPortfolio_WhitespaceBrokerName_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = controller.GetTransactionsByPortfolio(" ", "Default");

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
    }

    [Fact]
    public void TransactionsController_GetTransactionsByPortfolio_WhitespacePortfolioName_ReturnsBadRequest()
    {
        var controller = new TransactionsController(new StubTransactionService(), new StubTransactionQueryService());

        var result = controller.GetTransactionsByPortfolio("XPI", " ");

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestResult>();
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
        public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName) => throw new NotImplementedException();
        public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName) => throw new NotImplementedException();
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
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName) => throw new NotImplementedException();
        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName) => throw new NotImplementedException();
    }

    private sealed class StubDividendService : IDividendService
    {
        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request) => throw new NotImplementedException();
        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request) => throw new NotImplementedException();
    }
}
