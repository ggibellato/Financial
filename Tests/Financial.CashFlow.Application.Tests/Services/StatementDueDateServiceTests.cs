using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class StatementDueDateServiceTests
{
    private const string SpanName = "CashFlow.StatementDueDateService.SetDueDate";

    private readonly StubCashFlowRepository _repository = new(seedDefaultBanks: true);
    private readonly FakeCreditCardCalendarSyncService _calendarSync = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<StatementDueDateService> _logger = new();
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly Account _amex = TestAccounts.CreditCard("BA Amex");
    private readonly StatementDueDateService _sut;

    public StatementDueDateServiceTests()
    {
        _repository.Accounts.Add(_amex);
        AddChargeInvoiced(_amex, 2026, 9);
        AddChargeInvoiced(_amex, 2026, 10);
        AddChargeInvoiced(_amex, 2026, 11);
        _sut = CreateService();
    }

    private StatementDueDateService CreateService(TimeProvider? timeProvider = null) =>
        new(_repository, _calendarSync, timeProvider ?? TestClock.At(), _tracer, _logger);

    private void AddChargeInvoiced(Account card, int year, int month) =>
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(year, month, 2), "Charge", 30m, _groceries, null, card, invoiceDate: new DateOnly(year, month, 1)));

    private CardStatement StoredStatement(int month, DateOnly? dueDate = null)
    {
        var statement = CardStatement.Create(_amex, 2026, month);
        statement.SetDueDate(dueDate);
        _repository.CardStatements.Add(statement);
        return statement;
    }

    private Task SetAsync(int month, DateOnly? dueDate) =>
        _sut.SetDueDateAsync(_amex.Id, 2026, month, new StatementDueDateUpdateDTO { DueDate = dueDate });

    private void ShouldHaveRefused<TException>() where TException : Exception
    {
        using (new AssertionScope())
        {
            _repository.SaveChangesCallCount.Should().Be(0);
            _calendarSync.TriggeredCreditCardIds.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<TException>(SpanName);
        }
    }

    [Fact]
    public async Task SetDueDateAsync_StoredStatement_SetsTheDateSavesAndSyncsOnce()
    {
        var october = StoredStatement(10);

        await SetAsync(10, new DateOnly(2026, 10, 25));

        using (new AssertionScope())
        {
            october.DueDate.Should().Be(new DateOnly(2026, 10, 25));
            _repository.SaveChangesCallCount.Should().Be(1);
            _calendarSync.TriggeredCreditCardIds.Should().Equal(_amex.Id);
            _tracer.Spans.Should().ContainSingle(span => span.Name == SpanName)
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public async Task SetDueDateAsync_ListedMonthWithoutAStatement_CreatesItWithTheDate()
    {
        await SetAsync(10, new DateOnly(2026, 10, 25));

        var created = _repository.CardStatements.Should().ContainSingle().Which;
        using (new AssertionScope())
        {
            (created.Year, created.Month, created.IsPaid).Should().Be((2026, 10, false));
            created.DueDate.Should().Be(new DateOnly(2026, 10, 25));
            _calendarSync.TriggeredCreditCardIds.Should().Equal(_amex.Id);
        }
    }

    [Fact]
    public async Task SetDueDateAsync_NewDate_ClearsTheLaterStatementsDates()
    {
        var september = StoredStatement(9, new DateOnly(2026, 9, 18));
        var october = StoredStatement(10);
        var november = StoredStatement(11, new DateOnly(2026, 11, 18));

        await SetAsync(10, new DateOnly(2026, 10, 25));

        new[] { september.DueDate, october.DueDate, november.DueDate }.Should().Equal(
            new DateOnly(2026, 9, 18), new DateOnly(2026, 10, 25), null);
    }

    [Fact]
    public async Task SetDueDateAsync_Clearing_ClearsOnlyThatStatementSavesAndSyncs()
    {
        var september = StoredStatement(9, new DateOnly(2026, 9, 18));
        var october = StoredStatement(10, new DateOnly(2026, 10, 18));
        var november = StoredStatement(11, new DateOnly(2026, 11, 18));

        await SetAsync(10, null);

        using (new AssertionScope())
        {
            new[] { september.DueDate, october.DueDate, november.DueDate }.Should().Equal(
                new DateOnly(2026, 9, 18), null, new DateOnly(2026, 11, 18));
            _repository.SaveChangesCallCount.Should().Be(1);
            _calendarSync.TriggeredCreditCardIds.Should().Equal(_amex.Id);
        }
    }

    [Fact]
    public async Task SetDueDateAsync_ClearingAMonthWithNoStatement_WritesNothingButStillSyncs()
    {
        await SetAsync(10, null);

        using (new AssertionScope())
        {
            _repository.CardStatements.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _calendarSync.TriggeredCreditCardIds.Should().Equal(_amex.Id);
            _tracer.Spans.Should().ContainSingle(span => span.Name == SpanName)
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public async Task SetDueDateAsync_CurrentMonthBeforeItsFirstCharge_IsAccepted()
    {
        var sut = CreateService(TestClock.At(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero)));

        await sut.SetDueDateAsync(_amex.Id, 2026, 12, new StatementDueDateUpdateDTO { DueDate = new DateOnly(2026, 12, 20) });

        _repository.CardStatements.Should().ContainSingle().Which.DueDate.Should().Be(new DateOnly(2026, 12, 20));
    }

    [Fact]
    public async Task SetDueDateAsync_NullRequest_IsRefused()
    {
        var act = () => _sut.SetDueDateAsync(_amex.Id, 2026, 10, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        ShouldHaveRefused<ArgumentNullException>();
    }

    [Fact]
    public async Task SetDueDateAsync_UnknownAccount_IsRefused()
    {
        var act = () => _sut.SetDueDateAsync(Guid.NewGuid(), 2026, 10, new StatementDueDateUpdateDTO { DueDate = null });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Account '*' was not found.");
        ShouldHaveRefused<KeyNotFoundException>();
    }

    [Fact]
    public async Task SetDueDateAsync_AccountThatIsNotACard_IsRefused()
    {
        var barclays = _repository.Accounts.First(account => account.Name == "Barclays");

        var act = () => _sut.SetDueDateAsync(barclays.Id, 2026, 10, new StatementDueDateUpdateDTO { DueDate = null });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Barclays is not a card account.");
        ShouldHaveRefused<ArgumentException>();
    }

    [Fact]
    public async Task SetDueDateAsync_InactiveCard_IsRefused()
    {
        var inactive = TestAccounts.CreditCard("Old Visa", isActive: false);
        _repository.Accounts.Add(inactive);

        var act = () => _sut.SetDueDateAsync(inactive.Id, 2026, 10, new StatementDueDateUpdateDTO { DueDate = null });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Old Visa is inactive.");
        ShouldHaveRefused<ArgumentException>();
    }

    [Theory]
    [InlineData(2026, 12, "2026-12")]
    [InlineData(2026, 8, "2026-08")]
    [InlineData(2026, 13, "2026-13")]
    public async Task SetDueDateAsync_MonthTheStatementsTabDoesNotList_IsRefused(int year, int month, string label)
    {
        var act = () => _sut.SetDueDateAsync(_amex.Id, year, month, new StatementDueDateUpdateDTO { DueDate = null });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Card statement '{label}' was not found.");
        ShouldHaveRefused<KeyNotFoundException>();
    }

    [Theory]
    [InlineData(25)]
    [InlineData(null)]
    public async Task SetDueDateAsync_PaidStatement_IsRefusedForASetAndForAClear(int? day)
    {
        var october = StoredStatement(10, new DateOnly(2026, 10, 18));
        october.MarkPaidBy(Guid.NewGuid());

        var act = () => SetAsync(10, day is { } value ? new DateOnly(2026, 10, value) : null);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("The October 2026 statement is paid, so its due date can't change.");
        october.DueDate.Should().Be(new DateOnly(2026, 10, 18));
        ShouldHaveRefused<ArgumentException>();
    }

    [Fact]
    public async Task SetDueDateAsync_DateOutsideTheMonth_IsRefusedWithoutCreatingTheStatement()
    {
        var act = () => SetAsync(10, new DateOnly(2026, 11, 1));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("The due date must be in October 2026.");
        _repository.CardStatements.Should().BeEmpty();
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains("October"));
        ShouldHaveRefused<ArgumentException>();
    }

    [Fact]
    public async Task SetDueDateAsync_SaveFails_DoesNotSyncAndMarksTheSpanFailed()
    {
        StoredStatement(10);
        _repository.ThrowOnNextSave = true;

        var act = () => SetAsync(10, new DateOnly(2026, 10, 25));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _calendarSync.TriggeredCreditCardIds.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>(SpanName);
    }
}
