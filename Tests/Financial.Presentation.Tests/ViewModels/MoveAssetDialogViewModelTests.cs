using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Presentation.Tests.ViewModels;

/// <summary>
/// The dialog's own logic: which destinations it offers, and how that changes when the user chooses
/// to archive instead. Whether a destination is legal is the domain's to decide, so there is
/// nothing here asserting rules - only shape.
/// </summary>
public class MoveAssetDialogViewModelTests
{
    [Fact]
    public void OffersTheBrokersOtherPortfolios_NotTheOneTheAssetIsIn()
    {
        var sut = CreateViewModel();

        sut.AvailablePortfolios.Should().BeEquivalentTo(["ISA", "SIPP"]);
    }

    [Fact]
    public void WhenArchivingIsNotOffered_TheScopeChoiceStaysOff()
    {
        var sut = CreateViewModel(canArchive: false);

        using (new AssertionScope())
        {
            sut.CanArchive.Should().BeFalse();
            sut.ArchiveToHistoric.Should().BeFalse();
            sut.KeepInCurrentScope.Should().BeTrue();
        }
    }

    [Fact]
    public void ChoosingToArchive_SwapsTheDestinationsForTheHistoricOnes()
    {
        var sut = CreateViewModel(canArchive: true);

        sut.ArchiveToHistoric = true;

        sut.AvailablePortfolios.Should().BeEquivalentTo(["Closed", "Default"]);
    }

    [Fact]
    public void TheHistoricDestinationsKeepAPortfolioNamedLikeTheSource()
    {
        // Across scopes a Historic "Default" is a different portfolio from the Active one the asset
        // is leaving, so excluding it would hide a legitimate destination.
        var sut = CreateViewModel(canArchive: true);

        sut.ArchiveToHistoric = true;

        sut.AvailablePortfolios.Should().Contain("Default");
    }

    [Fact]
    public void GoingBackToTheCurrentScope_RestoresItsDestinations()
    {
        var sut = CreateViewModel(canArchive: true);

        sut.ArchiveToHistoric = true;
        sut.KeepInCurrentScope = true;

        using (new AssertionScope())
        {
            sut.AvailablePortfolios.Should().BeEquivalentTo(["ISA", "SIPP"]);
            sut.ArchiveToHistoric.Should().BeFalse();
        }
    }

    [Fact]
    public void WhenTheChosenScopeOffersNothing_StartsOnNamingANewPortfolio()
    {
        // An empty list is a dead end, so the dialog opens on the only route forward.
        var sut = CreateViewModel(canArchive: true, historic: []);

        sut.ArchiveToHistoric = true;

        using (new AssertionScope())
        {
            sut.HasExistingDestination.Should().BeFalse();
            sut.CreateNewPortfolio.Should().BeTrue();
            sut.ValidationMessage.Should().Be("Enter a name for the new portfolio.");
        }
    }

    [Fact]
    public void DestinationPortfolioName_TrimsATypedName()
    {
        var sut = CreateViewModel();

        sut.CreateNewPortfolio = true;
        sut.NewPortfolioName = "  Pension  ";

        using (new AssertionScope())
        {
            sut.DestinationPortfolioName.Should().Be("Pension");
            sut.ValidationMessage.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankNewName_BlocksConfirming(string name)
    {
        var sut = CreateViewModel();

        sut.CreateNewPortfolio = true;
        sut.NewPortfolioName = name;

        using (new AssertionScope())
        {
            sut.ValidationMessage.Should().Be("Enter a name for the new portfolio.");
            sut.ConfirmCommand.CanExecute(null).Should().BeFalse();
        }
    }

    [Fact]
    public void Confirm_WhenValid_ClosesWithTrue()
    {
        var sut = CreateViewModel();
        bool? result = null;
        sut.CloseRequested += (_, value) => result = value;

        sut.ConfirmCommand.Execute(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void Cancel_ClosesWithFalse()
    {
        var sut = CreateViewModel();
        bool? result = null;
        sut.CloseRequested += (_, value) => result = value;

        sut.CancelCommand.Execute(null);

        result.Should().BeFalse();
    }

    private static MoveAssetDialogViewModel CreateViewModel(
        bool canArchive = false,
        string[]? historic = null) =>
        new("Trading 212",
            "Default",
            "VUSA",
            ["Default", "ISA", "SIPP"],
            historic ?? ["Closed", "Default"],
            canArchive);
}
