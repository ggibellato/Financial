using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ExternalPaymentServiceTests
{
    private readonly StubCashFlowRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly ExternalPaymentService _sut;
    private readonly Account _barclays = TestAccounts.Bank("Barclays");

    public ExternalPaymentServiceTests()
    {
        _repository.Accounts.Add(_barclays);
        _sut = new ExternalPaymentService(_repository, _tracer, NullLogger<ExternalPaymentService>.Instance);
    }

    private static ExternalPaymentCreateDTO PayIn(string direction = "In", decimal amount = 20m) => new()
    {
        Direction = direction,
        Date = new DateOnly(2026, 11, 12),
        Amount = amount,
        Description = "Money from Ana"
    };

    [Theory]
    [InlineData("Sideways")]
    [InlineData("1")]
    public async Task AddPaymentAsync_UnknownDirection_ThrowsSavesNothingAndRecordsAFailedSpan(string direction)
    {
        var act = () => _sut.AddPaymentAsync(_barclays.Id, PayIn(direction));

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Choose whether money is paid in or out.");
            _repository.ExternalPayments.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExternalPaymentService.AddPayment");
        }
    }

    [Fact]
    public async Task UpdatePaymentAsync_OnAnotherAccountsPayment_ThrowsNotFound()
    {
        var payment = await _sut.AddPaymentAsync(_barclays.Id, PayIn());
        var chase = TestAccounts.Bank("Chase");
        _repository.Accounts.Add(chase);

        var act = () => _sut.UpdatePaymentAsync(chase.Id, payment.Id, new ExternalPaymentUpdateDTO
        {
            Date = new DateOnly(2026, 11, 13), Amount = 5m, Description = "Changed"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.ExternalPaymentService.UpdatePayment");
    }
}
