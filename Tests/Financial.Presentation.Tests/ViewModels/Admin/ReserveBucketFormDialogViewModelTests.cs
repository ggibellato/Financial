using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class ReserveBucketFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithEmptyFieldsAndActiveOn()
    {
        var viewModel = new ReserveBucketFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Reserve Bucket");
        viewModel.Name.Should().BeEmpty();
        viewModel.SplitPercentage.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new ReserveBucketFormDialogViewModel("Investimento", 33.33m, currentIsActive: false);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Reserve Bucket");
        viewModel.Name.Should().Be("Investimento");
        viewModel.SplitPercentage.Should().Be("33.33");
        viewModel.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_BlankFields_StartsInvalid()
    {
        var viewModel = new ReserveBucketFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ValidFields_BecomesValid()
    {
        var viewModel = new ReserveBucketFormDialogViewModel { Name = "Ferias", SplitPercentage = "20" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    [InlineData("not-a-number")]
    public void InvalidSplitPercentage_StaysInvalid(string splitPercentage)
    {
        var viewModel = new ReserveBucketFormDialogViewModel { Name = "Ferias", SplitPercentage = splitPercentage };

        viewModel.ValidationMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void ParsedSplitPercentage_ReturnsTheParsedDecimal()
    {
        var viewModel = new ReserveBucketFormDialogViewModel { Name = "Ferias", SplitPercentage = "20.5" };

        viewModel.ParsedSplitPercentage.Should().Be(20.5m);
    }

    [Fact]
    public void ConfirmCommand_ValidForm_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new ReserveBucketFormDialogViewModel { Name = "  Ferias  ", SplitPercentage = "20" };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("Ferias");
    }

    [Fact]
    public void ConfirmCommand_InvalidForm_DoesNotRaiseCloseRequested()
    {
        var viewModel = new ReserveBucketFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new ReserveBucketFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
