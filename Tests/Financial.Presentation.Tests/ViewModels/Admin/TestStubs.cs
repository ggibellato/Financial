using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.Tests.ViewModels.Admin;

internal sealed class StubBrokerService : IBrokerService
{
    public List<BrokerDTO> Brokers { get; set; } = [];
    public BrokerCreateDTO? LastCreateRequest { get; private set; }
    public (string CurrentName, BrokerUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public string? LastDeletedName { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<BrokerDTO> GetBrokers() => Brokers;

    public Task<BrokerDTO> CreateBrokerAsync(BrokerCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new BrokerDTO { Name = request.Name, Currency = request.Currency, Status = "Active", PortfolioCount = 0 };
        Brokers.Add(created);
        return Task.FromResult(created);
    }

    public Task<BrokerDTO> UpdateBrokerAsync(string currentName, BrokerUpdateDTO request)
    {
        LastUpdateRequest = (currentName, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new BrokerDTO { Name = request.Name, Currency = request.Currency, Status = "Active", PortfolioCount = 0 };
        return Task.FromResult(updated);
    }

    public Task DeleteBrokerAsync(string name)
    {
        LastDeletedName = name;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        Brokers.RemoveAll(b => b.Name == name);
        return Task.CompletedTask;
    }
}

internal sealed class StubPortfolioService : IPortfolioService
{
    public List<PortfolioDTO> Portfolios { get; set; } = [];
    public PortfolioCreateDTO? LastCreateRequest { get; private set; }
    public (string BrokerName, string CurrentName, PortfolioUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public (string BrokerName, string PortfolioName, InvestmentScope Scope)? LastDeleteRequest { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<PortfolioDTO> GetPortfolios() => Portfolios;

    public Task<PortfolioDTO> CreatePortfolioAsync(PortfolioCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new PortfolioDTO { Name = request.Name, BrokerName = request.BrokerName, BrokerStatus = "Active", AssetCount = 0 };
        Portfolios.Add(created);
        return Task.FromResult(created);
    }

    public Task<PortfolioDTO> UpdatePortfolioAsync(string brokerName, string currentName, PortfolioUpdateDTO request)
    {
        LastUpdateRequest = (brokerName, currentName, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new PortfolioDTO { Name = request.Name, BrokerName = brokerName, BrokerStatus = "Active", AssetCount = 0 };
        return Task.FromResult(updated);
    }

    public Task DeleteEmptyPortfolioAsync(string brokerName, string portfolioName, InvestmentScope scope)
    {
        LastDeleteRequest = (brokerName, portfolioName, scope);
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        Portfolios.RemoveAll(p => p.BrokerName == brokerName && p.Name == portfolioName);
        return Task.CompletedTask;
    }
}

/// <summary>Records the dialog it was shown and returns a caller-configured result, avoiding a real
/// modal Window in tests.</summary>
internal sealed class StubDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public string? LastConfirmMessage { get; private set; }

    public bool ShowBrokerFormDialogResult { get; set; } = true;
    public BrokerFormDialogViewModel? LastBrokerFormDialog { get; private set; }
    /// <summary>Applied to the dialog ViewModel before the caller reads it back, simulating what the
    /// user typed while the (never-rendered) modal was open.</summary>
    public Action<BrokerFormDialogViewModel>? OnShowBrokerFormDialog { get; set; }

    public bool ShowPortfolioFormDialogResult { get; set; } = true;
    public PortfolioFormDialogViewModel? LastPortfolioFormDialog { get; private set; }
    public Action<PortfolioFormDialogViewModel>? OnShowPortfolioFormDialog { get; set; }

    public bool Confirm(string message, string caption)
    {
        LastConfirmMessage = message;
        return ConfirmResult;
    }

    public void ShowWarning(string message, string caption)
    {
    }

    public bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel) => throw new NotImplementedException();

    public bool ShowBrokerFormDialog(BrokerFormDialogViewModel viewModel)
    {
        LastBrokerFormDialog = viewModel;
        OnShowBrokerFormDialog?.Invoke(viewModel);
        return ShowBrokerFormDialogResult;
    }

    public bool ShowPortfolioFormDialog(PortfolioFormDialogViewModel viewModel)
    {
        LastPortfolioFormDialog = viewModel;
        OnShowPortfolioFormDialog?.Invoke(viewModel);
        return ShowPortfolioFormDialogResult;
    }
}
