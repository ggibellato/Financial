using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.Tests.ViewModels.Admin;

internal sealed class StubBankService : IBankService
{
    public List<BankDTO> Banks { get; set; } = [];
    public BankCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, BankUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<BankDTO> GetBanks() => Banks;

    public Task<BankDTO> CreateBankAsync(BankCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new BankDTO
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RoundUpEnabled = request.RoundUpEnabled,
            OpeningBalance = 0,
            OpeningBalanceDate = default,
            HasReferences = false,
        };
        Banks.Add(created);
        return Task.FromResult(created);
    }

    public Task<BankDTO> UpdateBankAsync(Guid id, BankUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new BankDTO
        {
            Id = id,
            Name = request.Name,
            RoundUpEnabled = request.RoundUpEnabled,
            OpeningBalance = 0,
            OpeningBalanceDate = default,
            HasReferences = false,
        };
        return Task.FromResult(updated);
    }

    public Task DeleteBankAsync(Guid id)
    {
        LastDeletedId = id;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        Banks.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }

    public Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request) => throw new NotImplementedException();

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => throw new NotImplementedException();

    public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => throw new NotImplementedException();
}

internal sealed class StubCategoryService : ICategoryService
{
    public List<CategoryDTO> Categories { get; set; } = [];
    public CategoryCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, CategoryUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<CategoryDTO> GetCategories() => Categories;

    public Task<CategoryDTO> CreateCategoryAsync(CategoryCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new CategoryDTO
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Active = request.Active,
            IsInvestment = request.IsInvestment,
            IsTithe = request.IsTithe,
            HasReferences = false,
        };
        Categories.Add(created);
        return Task.FromResult(created);
    }

    public Task<CategoryDTO> UpdateCategoryAsync(Guid id, CategoryUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new CategoryDTO
        {
            Id = id,
            Name = request.Name,
            Active = request.Active,
            IsInvestment = request.IsInvestment,
            IsTithe = request.IsTithe,
            HasReferences = false,
        };
        return Task.FromResult(updated);
    }

    public Task DeleteCategoryAsync(Guid id)
    {
        LastDeletedId = id;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        Categories.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class StubCreditCardService : ICreditCardService
{
    public List<CreditCardDTO> CreditCards { get; set; } = [];
    public CreditCardCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, CreditCardUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<CreditCardDTO> GetCreditCards() => CreditCards;

    public Task<CreditCardDTO> CreateCreditCardAsync(CreditCardCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new CreditCardDTO
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsActive = request.IsActive,
            NextInvoiceDueDate = null,
            HasReferences = false,
        };
        CreditCards.Add(created);
        return Task.FromResult(created);
    }

    public Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new CreditCardDTO
        {
            Id = id,
            Name = request.Name,
            IsActive = request.IsActive,
            NextInvoiceDueDate = request.NextInvoiceDueDate,
            HasReferences = false,
        };
        return Task.FromResult(updated);
    }

    public Task DeleteCreditCardAsync(Guid id)
    {
        LastDeletedId = id;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        CreditCards.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class StubIncomeSourceService : IIncomeSourceService
{
    public List<IncomeSourceDTO> IncomeSources { get; set; } = [];
    public IncomeSourceCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, IncomeSourceUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources() => IncomeSources;

    public Task<IncomeSourceDTO> CreateIncomeSourceAsync(IncomeSourceCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new IncomeSourceDTO
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Group = request.Group,
            IsActive = request.IsActive,
            AutoSplitToReserve = request.AutoSplitToReserve,
            HasReferences = false,
        };
        IncomeSources.Add(created);
        return Task.FromResult(created);
    }

    public Task<IncomeSourceDTO> UpdateIncomeSourceAsync(Guid id, IncomeSourceUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new IncomeSourceDTO
        {
            Id = id,
            Name = request.Name,
            Group = request.Group,
            IsActive = request.IsActive,
            AutoSplitToReserve = request.AutoSplitToReserve,
            HasReferences = false,
        };
        return Task.FromResult(updated);
    }

    public Task DeleteIncomeSourceAsync(Guid id)
    {
        LastDeletedId = id;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        IncomeSources.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class StubMensaisService : IMensaisService
{
    public List<RecurringBillDTO> Bills { get; set; } = [];
    public RecurringBillCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, RecurringBillUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }
    public Exception? ThrowOnDelete { get; set; }

    public IReadOnlyList<RecurringBillDTO> GetBills() => Bills;

    public Task<RecurringBillDTO> CreateBillAsync(RecurringBillCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new RecurringBillDTO
        {
            Id = Guid.NewGuid(),
            DueDay = request.DueDay,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note,
            NitNumber = null,
            MinimumWageValue = null,
            Status = "Unset",
        };
        Bills.Add(created);
        return Task.FromResult(created);
    }

    public Task<RecurringBillDTO> UpdateBillAsync(Guid id, RecurringBillUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new RecurringBillDTO
        {
            Id = id,
            DueDay = request.DueDay,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note,
            NitNumber = request.NitNumber,
            MinimumWageValue = request.MinimumWageValue,
            Status = request.Status,
        };
        return Task.FromResult(updated);
    }

    public Task DeleteBillAsync(Guid id)
    {
        LastDeletedId = id;
        if (ThrowOnDelete is not null)
        {
            throw ThrowOnDelete;
        }

        Bills.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync() => throw new NotSupportedException();
}

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

internal sealed class StubAssetAdminService : IAssetAdminService
{
    public List<AssetAdminDTO> Assets { get; set; } = [];
    public AssetAdminCreateDTO? LastCreateRequest { get; private set; }
    public (string BrokerName, string PortfolioName, string CurrentName, AssetAdminUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Exception? ThrowOnCreate { get; set; }
    public Exception? ThrowOnUpdate { get; set; }

    public IReadOnlyList<AssetAdminDTO> GetAssets() => Assets;

    public Task<AssetAdminDTO> CreateAssetAsync(AssetAdminCreateDTO request)
    {
        LastCreateRequest = request;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        var created = new AssetAdminDTO
        {
            Name = request.Name,
            BrokerName = request.BrokerName,
            PortfolioName = request.PortfolioName,
            BrokerStatus = "Active",
            ISIN = request.ISIN,
            Exchange = request.Exchange,
            Ticker = request.Ticker,
            Country = request.Country,
            LocalTypeCode = request.LocalTypeCode,
            Class = request.Class ?? Financial.Investment.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 0,
        };
        Assets.Add(created);
        return Task.FromResult(created);
    }

    public Task<AssetAdminDTO> UpdateAssetAsync(string brokerName, string portfolioName, string currentName, AssetAdminUpdateDTO request)
    {
        LastUpdateRequest = (brokerName, portfolioName, currentName, request);
        if (ThrowOnUpdate is not null)
        {
            throw ThrowOnUpdate;
        }

        var updated = new AssetAdminDTO
        {
            Name = request.Name,
            BrokerName = brokerName,
            PortfolioName = portfolioName,
            BrokerStatus = "Active",
            ISIN = request.ISIN,
            Exchange = request.Exchange,
            Ticker = request.Ticker,
            Country = request.Country,
            LocalTypeCode = request.LocalTypeCode,
            Class = request.Class,
            Quantity = 0,
        };
        return Task.FromResult(updated);
    }
}

internal sealed class StubAssetMoveService : IAssetMoveService
{
    public ArchiveAssetRequestDTO? LastArchiveRequest { get; private set; }
    public Exception? ThrowOnArchive { get; set; }

    public Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request) => throw new NotImplementedException();

    public Task<AssetDetailsDTO> ArchiveAssetAsync(ArchiveAssetRequestDTO request)
    {
        LastArchiveRequest = request;
        if (ThrowOnArchive is not null)
        {
            throw ThrowOnArchive;
        }

        return Task.FromResult(new AssetDetailsDTO
        {
            Name = request.AssetName,
            BrokerName = request.BrokerName,
            PortfolioName = request.DestinationPortfolioName,
            Ticker = string.Empty,
        });
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

    public bool ShowAssetFormDialogResult { get; set; } = true;
    public AssetFormDialogViewModel? LastAssetFormDialog { get; private set; }
    public Action<AssetFormDialogViewModel>? OnShowAssetFormDialog { get; set; }

    public bool ShowBankFormDialogResult { get; set; } = true;
    public BankFormDialogViewModel? LastBankFormDialog { get; private set; }
    public Action<BankFormDialogViewModel>? OnShowBankFormDialog { get; set; }

    public bool ShowCategoryFormDialogResult { get; set; } = true;
    public CategoryFormDialogViewModel? LastCategoryFormDialog { get; private set; }
    public Action<CategoryFormDialogViewModel>? OnShowCategoryFormDialog { get; set; }

    public bool ShowCreditCardFormDialogResult { get; set; } = true;
    public CreditCardFormDialogViewModel? LastCreditCardFormDialog { get; private set; }
    public Action<CreditCardFormDialogViewModel>? OnShowCreditCardFormDialog { get; set; }

    public bool ShowIncomeSourceFormDialogResult { get; set; } = true;
    public IncomeSourceFormDialogViewModel? LastIncomeSourceFormDialog { get; private set; }
    public Action<IncomeSourceFormDialogViewModel>? OnShowIncomeSourceFormDialog { get; set; }

    public bool ShowRecurringBillFormDialogResult { get; set; } = true;
    public RecurringBillFormDialogViewModel? LastRecurringBillFormDialog { get; private set; }
    public Action<RecurringBillFormDialogViewModel>? OnShowRecurringBillFormDialog { get; set; }

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

    public bool ShowAssetFormDialog(AssetFormDialogViewModel viewModel)
    {
        LastAssetFormDialog = viewModel;
        OnShowAssetFormDialog?.Invoke(viewModel);
        return ShowAssetFormDialogResult;
    }

    public bool ShowBankFormDialog(BankFormDialogViewModel viewModel)
    {
        LastBankFormDialog = viewModel;
        OnShowBankFormDialog?.Invoke(viewModel);
        return ShowBankFormDialogResult;
    }

    public bool ShowCategoryFormDialog(CategoryFormDialogViewModel viewModel)
    {
        LastCategoryFormDialog = viewModel;
        OnShowCategoryFormDialog?.Invoke(viewModel);
        return ShowCategoryFormDialogResult;
    }

    public bool ShowCreditCardFormDialog(CreditCardFormDialogViewModel viewModel)
    {
        LastCreditCardFormDialog = viewModel;
        OnShowCreditCardFormDialog?.Invoke(viewModel);
        return ShowCreditCardFormDialogResult;
    }

    public bool ShowIncomeSourceFormDialog(IncomeSourceFormDialogViewModel viewModel)
    {
        LastIncomeSourceFormDialog = viewModel;
        OnShowIncomeSourceFormDialog?.Invoke(viewModel);
        return ShowIncomeSourceFormDialogResult;
    }

    public bool ShowRecurringBillFormDialog(RecurringBillFormDialogViewModel viewModel)
    {
        LastRecurringBillFormDialog = viewModel;
        OnShowRecurringBillFormDialog?.Invoke(viewModel);
        return ShowRecurringBillFormDialogResult;
    }
}
