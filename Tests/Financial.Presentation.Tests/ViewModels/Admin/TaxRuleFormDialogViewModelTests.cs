using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class TaxRuleFormDialogViewModelTests
{
    private static TaxRuleDTO Rule(
        Jurisdiction jurisdiction = Jurisdiction.UK,
        EventCategory eventCategory = EventCategory.Interest,
        string label = "UK interest rule",
        string description = "Some notes",
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveTo = null) => new()
    {
        Id = Guid.NewGuid(),
        Jurisdiction = jurisdiction,
        EventCategory = eventCategory,
        Label = label,
        Description = description,
        EffectiveFrom = effectiveFrom ?? new DateOnly(2026, 1, 1),
        EffectiveTo = effectiveTo ?? new DateOnly(2026, 12, 31),
    };

    [Fact]
    public void CreateMode_DefaultsToEmptyLabelAndIsCreateModeTrue()
    {
        var vm = new TaxRuleFormDialogViewModel();

        vm.IsEditing.Should().BeFalse();
        vm.IsCreateMode.Should().BeTrue();
        vm.Title.Should().Be("Create Tax Rule");
        vm.Label.Should().BeEmpty();
    }

    [Fact]
    public void EditMode_PreFillsFromTheExistingRuleAndLocksJurisdictionAndCategory()
    {
        var rule = Rule();
        var vm = new TaxRuleFormDialogViewModel(rule);

        vm.IsEditing.Should().BeTrue();
        vm.IsCreateMode.Should().BeFalse();
        vm.Title.Should().Be("Edit Tax Rule");
        vm.Jurisdiction.Should().Be(Jurisdiction.UK);
        vm.EventCategory.Should().Be(EventCategory.Interest);
        vm.Label.Should().Be("UK interest rule");
        vm.Description.Should().Be("Some notes");
        vm.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        vm.EffectiveTo.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void BlankLabel_DisablesConfirmWithAValidationMessage()
    {
        var vm = new TaxRuleFormDialogViewModel { Label = "   ", EffectiveFrom = new DateOnly(2026, 1, 1) };

        vm.ValidationMessage.Should().Be("Label is required.");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void NoEffectiveFrom_DisablesConfirmWithAValidationMessage()
    {
        var vm = new TaxRuleFormDialogViewModel { Label = "A rule" };

        vm.ValidationMessage.Should().Be("Effective From is required.");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EffectiveFromOnOrAfterEffectiveTo_DisablesConfirmWithAValidationMessage()
    {
        var vm = new TaxRuleFormDialogViewModel
        {
            Label = "A rule",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = new DateOnly(2026, 1, 1),
        };

        vm.ValidationMessage.Should().Be("Effective From must be before Effective To.");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ValidInput_EnablesConfirm()
    {
        var vm = new TaxRuleFormDialogViewModel { Label = "A rule", EffectiveFrom = new DateOnly(2026, 1, 1) };

        vm.ValidationMessage.Should().BeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Confirm_TrimsTheLabelAndRaisesCloseRequestedWithTrue()
    {
        var vm = new TaxRuleFormDialogViewModel { Label = "  A rule  ", EffectiveFrom = new DateOnly(2026, 1, 1) };
        bool? result = null;
        vm.CloseRequested += (_, r) => result = r;

        vm.ConfirmCommand.Execute(null);

        result.Should().BeTrue();
        vm.Label.Should().Be("A rule");
    }

    [Fact]
    public void Cancel_RaisesCloseRequestedWithFalse()
    {
        var vm = new TaxRuleFormDialogViewModel();
        bool? result = null;
        vm.CloseRequested += (_, r) => result = r;

        vm.CancelCommand.Execute(null);

        result.Should().BeFalse();
    }
}
