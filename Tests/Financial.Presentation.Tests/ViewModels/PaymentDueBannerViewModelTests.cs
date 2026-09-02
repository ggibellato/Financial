using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class PaymentDueBannerViewModelTests
{
    private static PaymentDueDTO Payment(string name, int daysRemaining = 3) => new()
    {
        Type = "Mensais",
        Name = name,
        DueDate = new DateOnly(2026, 9, 5),
        DaysRemaining = daysRemaining,
    };

    private static PaymentDueBannerViewModel CreateViewModel(StubPaymentsDueService? service = null) =>
        new(service ?? new StubPaymentsDueService());

    [Fact]
    public void Constructor_WithNullService_Throws()
    {
        Action act = () => new PaymentDueBannerViewModel(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("paymentsDueService");
    }

    [Fact]
    public void Constructor_FetchesPaymentsImmediately()
    {
        var service = new StubPaymentsDueService { PaymentsToReturn = [Payment("Internet"), Payment("Rent")] };

        var vm = CreateViewModel(service);

        vm.Payments.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithEmptyResponse_IsVisibleIsFalse()
    {
        var vm = CreateViewModel(new StubPaymentsDueService { PaymentsToReturn = [] });

        vm.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNonEmptyResponse_IsVisibleIsTrue()
    {
        var vm = CreateViewModel(new StubPaymentsDueService { PaymentsToReturn = [Payment("Internet")] });

        vm.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Payments_MapEachDtoToARowViewModel_InOrder()
    {
        var service = new StubPaymentsDueService { PaymentsToReturn = [Payment("First"), Payment("Second"), Payment("Third")] };

        var vm = CreateViewModel(service);

        vm.Payments.Select(p => p.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public void Dismiss_SetsIsVisibleToFalse()
    {
        var vm = CreateViewModel(new StubPaymentsDueService { PaymentsToReturn = [Payment("Internet")] });

        vm.Dismiss();

        vm.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void DismissCommand_Execute_SetsIsVisibleToFalse()
    {
        var vm = CreateViewModel(new StubPaymentsDueService { PaymentsToReturn = [Payment("Internet")] });

        vm.DismissCommand.Execute(null);

        vm.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void Dismiss_RaisesPropertyChangedForIsVisible()
    {
        var vm = CreateViewModel(new StubPaymentsDueService { PaymentsToReturn = [Payment("Internet")] });
        var raised = false;
        vm.PropertyChanged += (_, e) => raised = e.PropertyName == nameof(vm.IsVisible);

        vm.Dismiss();

        raised.Should().BeTrue();
    }
}
