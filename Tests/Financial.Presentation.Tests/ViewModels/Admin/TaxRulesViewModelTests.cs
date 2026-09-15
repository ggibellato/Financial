using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class TaxRulesViewModelTests
{
    private static (TaxRulesViewModel ViewModel, StubTaxRuleService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubTaxRuleService();
        var dialog = new StubDialogService();
        var viewModel = new TaxRulesViewModel(service, dialog, new RecordingLogger<TaxRulesViewModel>());
        return (viewModel, service, dialog);
    }

    private static TaxRuleDTO Rule(Guid id, string label = "BR dividend withholding") => new()
    {
        Id = id,
        Jurisdiction = Jurisdiction.BR,
        EventCategory = EventCategory.Dividend,
        Label = label,
        Description = string.Empty,
        EffectiveFrom = new DateOnly(2026, 1, 1),
        EffectiveTo = null,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesTaxRulesFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.TaxRules = [Rule(Guid.NewGuid())];

        await viewModel.RefreshAsync();

        viewModel.TaxRules.Should().ContainSingle(r => r.Label == "BR dividend withholding");
        viewModel.HasNoTaxRules.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WithNoRules_HasNoTaxRulesIsTrue()
    {
        var (viewModel, _, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.HasNoTaxRules.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTaxRuleAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowTaxRuleFormDialogResult = true;
        dialog.OnShowTaxRuleFormDialog = vm =>
        {
            vm.Jurisdiction = Jurisdiction.UK;
            vm.EventCategory = EventCategory.Interest;
            vm.Label = "UK interest rule";
            vm.EffectiveFrom = new DateOnly(2026, 1, 1);
        };

        await viewModel.CreateTaxRuleAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Jurisdiction.Should().Be(Jurisdiction.UK);
        service.LastCreateRequest.EventCategory.Should().Be(EventCategory.Interest);
        service.LastCreateRequest.Label.Should().Be("UK interest rule");
        viewModel.TaxRules.Should().ContainSingle(r => r.Label == "UK interest rule");
    }

    [Fact]
    public async Task CreateTaxRuleAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowTaxRuleFormDialogResult = false;

        await viewModel.CreateTaxRuleAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateTaxRuleAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowTaxRuleFormDialog = vm =>
        {
            vm.Label = "Overlapping rule";
            vm.EffectiveFrom = new DateOnly(2026, 1, 1);
        };
        service.ThrowOnCreate = new InvalidOperationException("This range overlaps existing rule \"BR dividend withholding\" (2026-01-01–present).");

        await viewModel.CreateTaxRuleAsync();

        viewModel.ActionError.Should().Be("This range overlaps existing rule \"BR dividend withholding\" (2026-01-01–present).");
    }

    [Fact]
    public async Task EditTaxRuleAsync_PreFillsDialogAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var rule = Rule(id);
        dialog.OnShowTaxRuleFormDialog = vm => vm.Label = "Renamed rule";

        await viewModel.EditTaxRuleAsync(rule);

        dialog.LastTaxRuleFormDialog!.Jurisdiction.Should().Be(Jurisdiction.BR);
        dialog.LastTaxRuleFormDialog.EventCategory.Should().Be(EventCategory.Dividend);
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.Label.Should().Be("Renamed rule");
    }

    [Fact]
    public async Task EditTaxRuleAsync_NullRule_DoesNothing()
    {
        var (viewModel, service, dialog) = CreateViewModel();

        await viewModel.EditTaxRuleAsync(null);

        service.LastUpdateRequest.Should().BeNull();
        dialog.LastTaxRuleFormDialog.Should().BeNull();
    }

    [Fact]
    public async Task EditTaxRuleAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowTaxRuleFormDialogResult = false;
        var rule = Rule(Guid.NewGuid());

        await viewModel.EditTaxRuleAsync(rule);

        service.LastUpdateRequest.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_ConfirmsThenDeletes_WithPermanentDeletionWording()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var rule = Rule(id);

        await viewModel.DeleteTaxRuleAsync(rule);

        dialog.LastConfirmMessage.Should().Contain("permanently deleted");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var rule = Rule(Guid.NewGuid());

        await viewModel.DeleteTaxRuleAsync(rule);

        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_NullRule_DoesNothing()
    {
        var (viewModel, service, _) = CreateViewModel();

        await viewModel.DeleteTaxRuleAsync(null);

        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_ServiceRejectsWithFinalClassificationGuard_SurfacesActionErrorWithoutRemovingTheRow()
    {
        var (viewModel, service, _) = CreateViewModel();
        var id = Guid.NewGuid();
        var rule = Rule(id);
        service.TaxRules = [rule];
        await viewModel.RefreshAsync();
        service.ThrowOnDelete = new InvalidOperationException(
            "Cannot delete tax rule \"BR dividend withholding\" while a final classification still depends on it, for tax year(s): 2026.");

        await viewModel.DeleteTaxRuleAsync(rule);

        viewModel.ActionError.Should().Be(
            "Cannot delete tax rule \"BR dividend withholding\" while a final classification still depends on it, for tax year(s): 2026.");
        viewModel.TaxRules.Should().ContainSingle(r => r.Id == id);
    }
}
