using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionsTabViewModelTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    private static (TransactionsTabViewModel ViewModel, StubTransactionService Service, Spy Spy) Build(
        bool hasContext = true,
        ITransactionService? service = null)
    {
        var stubService = service as StubTransactionService ?? new StubTransactionService();
        var spy = new Spy();
        var viewModel = new TransactionsTabViewModel(
            stubService,
            new StubTransactionQueryService(),
            InvestmentScope.Active,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        return (viewModel, stubService, spy);
    }

    private static TransactionDialogData ValidDialogData(Guid? id = null) => new(
        TransactionId: id ?? Guid.NewGuid(),
        Date: DateTime.Today,
        Type: "Buy",
        Quantity: 10m,
        UnitPrice: 25m,
        Fees: 1.5m);

    private static Task<TransactionDialogData?> AsForm(TransactionDialogData? data) => Task.FromResult(data);

    [Fact]
    public async Task Add_NoContext_ShowsInfoAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build(hasContext: false);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Information);
    }

    [Fact]
    public async Task Add_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, spy) = Build();

        await viewModel.Add(() => AsForm(null));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();

        await viewModel.Add(() => AsForm(ValidDialogData() with { Type = "NotAType" }));

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Add_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { AddResult = null };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Add_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { AddResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);

        await viewModel.Add(() => AsForm(ValidDialogData()));

        service.LastAddRequest.Should().NotBeNull();
        service.LastAddRequest!.BrokerName.Should().Be(BrokerName);
        service.LastAddRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastAddRequest.AssetName.Should().Be(AssetName);
        service.LastAddRequest.Type.Should().Be("Buy");
        service.LastAddRequest.Quantity.Should().Be(10m);

        service.LastAddRequest.UnitPrice.Should().Be(25m);
        service.LastAddRequest.Fees.Should().Be(1.5m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task AddTransactionCommand_AfterSuccessfulAdd_PersistsDateAndTypeForNextOpen()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { AddResult = expectedDetails };
        var (viewModel, _, _) = Build(service: service);
        var usedDate = DateTime.Today.AddDays(-3);

        await viewModel.Add(() => AsForm(ValidDialogData() with { Date = usedDate, Type = "Sell" }));

        viewModel.AddTransactionCommand.Execute(null);

        viewModel.TransactionFormViewModel!.Date.Should().Be(usedDate);
        viewModel.TransactionFormViewModel!.Type.Should().Be("Sell");
    }

    [Fact]
    public async Task Update_NullSelectedTransaction_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Update(null, () => AsForm(ValidDialogData()));

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData()));

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(null));

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(selected.Id) with { Type = "NotAType" }));

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { UpdateResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(id) with { Type = "Sell", Quantity = 3m }));

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Id.Should().Be(id);
        service.LastUpdateRequest.Type.Should().Be("Sell");
        service.LastUpdateRequest.Quantity.Should().Be(3m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Update_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { UpdateResult = null };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Update(selected, () => AsForm(ValidDialogData(id)));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NullSelectedTransaction_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();

        await viewModel.Delete(null, () => true);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (viewModel, service, spy) = Build();
        var selected = new TransactionDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        service.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Delete_NotConfirmed_DoesNotCallService()
    {
        var (viewModel, service, _) = Build();
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => false);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_Success_PassesCorrectIdAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubTransactionService { DeleteResult = expectedDetails };
        var (viewModel, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new TransactionDTO { Id = id, Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        service.LastDeleteRequest.Should().NotBeNull();
        service.LastDeleteRequest!.Id.Should().Be(id);
        service.LastDeleteRequest.BrokerName.Should().Be(BrokerName);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Delete_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubTransactionService { DeleteResult = null };
        var (viewModel, _, spy) = Build(service: service);
        var selected = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        await viewModel.Delete(selected, () => true);

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    private sealed class Spy
    {
        public AssetDetailsDTO? AppliedDetails { get; private set; }
        public List<(string Message, string Caption, MessageBoxImage Image)> Messages { get; } = [];

        public void ApplyDetails(AssetDetailsDTO details) => AppliedDetails = details;
        public void ShowMessage(string message, string caption, MessageBoxImage image) => Messages.Add((message, caption, image));
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public AssetDetailsDTO? AddResult { get; set; }
        public AssetDetailsDTO? UpdateResult { get; set; }
        public AssetDetailsDTO? DeleteResult { get; set; }
        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public TransactionCreateDTO? LastAddRequest { get; private set; }
        public TransactionUpdateDTO? LastUpdateRequest { get; private set; }
        public TransactionDeleteDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request)
        {
            AddCallCount++;
            LastAddRequest = request;
            return Task.FromResult(AddResult);
        }

        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request)
        {
            UpdateCallCount++;
            LastUpdateRequest = request;
            return Task.FromResult(UpdateResult);
        }

        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request)
        {
            DeleteCallCount++;
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }
    }
}
