using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class CreditActionsTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BBAS3";

    private static (CreditActions Actions, StubCreditService Service, Spy Spy) Build(
        bool hasContext = true,
        ICreditService? service = null)
    {
        var stubService = service as StubCreditService ?? new StubCreditService();
        var spy = new Spy();
        var actions = new CreditActions(
            stubService,
            () => hasContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            spy.ApplyDetails,
            spy.ShowMessage);
        return (actions, stubService, spy);
    }

    private static CreditDialogData ValidDialogData(Guid? id = null) => new(
        CreditId: id ?? Guid.NewGuid(),
        Date: DateTime.Today,
        Type: "Dividend",
        Value: 12.5m);

    // --- Add ---

    [Fact]
    public async Task Add_NoContext_ShowsInfoAndDoesNotCallService()
    {
        var (actions, service, spy) = Build(hasContext: false);

        await actions.Add(() => ValidDialogData());

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Information);
    }

    [Fact]
    public async Task Add_DialogCancelled_DoesNotCallService()
    {
        var (actions, service, spy) = Build();

        await actions.Add(() => null);

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (actions, service, spy) = Build();

        await actions.Add(() => ValidDialogData() with { Type = "NotAType" });

        service.AddCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Add_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubCreditService { AddResult = null };
        var (actions, _, spy) = Build(service: service);

        await actions.Add(() => ValidDialogData());

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    [Fact]
    public async Task Add_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCreditService { AddResult = expectedDetails };
        var (actions, _, spy) = Build(service: service);

        await actions.Add(() => ValidDialogData());

        service.LastAddRequest.Should().NotBeNull();
        service.LastAddRequest!.BrokerName.Should().Be(BrokerName);
        service.LastAddRequest.PortfolioName.Should().Be(PortfolioName);
        service.LastAddRequest.AssetName.Should().Be(AssetName);
        service.LastAddRequest.Type.Should().Be("Dividend");
        service.LastAddRequest.Value.Should().Be(12.5m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    // --- Update ---

    [Fact]
    public async Task Update_NullSelectedCredit_DoesNotCallService()
    {
        var (actions, service, _) = Build();

        await actions.Update(null, () => ValidDialogData());

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (actions, service, spy) = Build();
        var selected = new CreditDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Update(selected, () => ValidDialogData());

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_DialogCancelled_DoesNotCallService()
    {
        var (actions, service, _) = Build();
        var selected = new CreditDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Update(selected, () => null);

        service.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_InvalidType_ShowsWarningAndDoesNotCallService()
    {
        var (actions, service, spy) = Build();
        var selected = new CreditDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Update(selected, () => ValidDialogData(selected.Id) with { Type = "NotAType" });

        service.UpdateCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Update_Success_PassesCorrectRequestAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCreditService { UpdateResult = expectedDetails };
        var (actions, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new CreditDTO { Id = id, Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Update(selected, () => ValidDialogData(id) with { Type = "Rent", Value = 99m });

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Id.Should().Be(id);
        service.LastUpdateRequest.Type.Should().Be("Rent");
        service.LastUpdateRequest.Value.Should().Be(99m);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Update_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubCreditService { UpdateResult = null };
        var (actions, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new CreditDTO { Id = id, Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Update(selected, () => ValidDialogData(id));

        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
        spy.AppliedDetails.Should().BeNull();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_NullSelectedCredit_DoesNotCallService()
    {
        var (actions, service, _) = Build();

        await actions.Delete(null, () => true);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_EmptyId_ShowsWarningAndDoesNotCallService()
    {
        var (actions, service, spy) = Build();
        var selected = new CreditDTO { Id = Guid.Empty, Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Delete(selected, () => true);

        service.DeleteCallCount.Should().Be(0);
        spy.Messages.Should().ContainSingle(m => m.Image == MessageBoxImage.Warning);
    }

    [Fact]
    public async Task Delete_NotConfirmed_DoesNotCallService()
    {
        var (actions, service, _) = Build();
        var selected = new CreditDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Delete(selected, () => false);

        service.DeleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_Success_PassesCorrectIdAndAppliesReturnedDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = AssetName, BrokerName = BrokerName, PortfolioName = PortfolioName, Ticker = "T" };
        var service = new StubCreditService { DeleteResult = expectedDetails };
        var (actions, _, spy) = Build(service: service);
        var id = Guid.NewGuid();
        var selected = new CreditDTO { Id = id, Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Delete(selected, () => true);

        service.LastDeleteRequest.Should().NotBeNull();
        service.LastDeleteRequest!.Id.Should().Be(id);
        service.LastDeleteRequest.BrokerName.Should().Be(BrokerName);
        spy.AppliedDetails.Should().Be(expectedDetails);
    }

    [Fact]
    public async Task Delete_ServiceReturnsNull_ShowsWarningAndDoesNotApplyDetails()
    {
        var service = new StubCreditService { DeleteResult = null };
        var (actions, _, spy) = Build(service: service);
        var selected = new CreditDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 1m };

        await actions.Delete(selected, () => true);

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

    private sealed class StubCreditService : ICreditService
    {
        public AssetDetailsDTO? AddResult { get; set; }
        public AssetDetailsDTO? UpdateResult { get; set; }
        public AssetDetailsDTO? DeleteResult { get; set; }
        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public CreditCreateDTO? LastAddRequest { get; private set; }
        public CreditUpdateDTO? LastUpdateRequest { get; private set; }
        public CreditDeleteDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request)
        {
            AddCallCount++;
            LastAddRequest = request;
            return Task.FromResult(AddResult);
        }

        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request)
        {
            UpdateCallCount++;
            LastUpdateRequest = request;
            return Task.FromResult(UpdateResult);
        }

        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request)
        {
            DeleteCallCount++;
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }
    }
}
