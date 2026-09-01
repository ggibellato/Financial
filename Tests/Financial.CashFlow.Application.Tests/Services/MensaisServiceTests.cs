using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class MensaisServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<MensaisService> Logger = NullLogger<MensaisService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly MensaisService _sut;

    public MensaisServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private MensaisService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new MensaisService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new MensaisService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task CreateBillAsync_WithValidRequest_SavesAndReturnsBill()
    {
        var result = await _sut.CreateBillAsync(ValidBrasilRequest());

        using (new AssertionScope())
        {
            result.Description.Should().Be("INSS");
            result.Area.Should().Be("Brasil");
            result.Status.Should().Be("Unset");
            _repository.RecurringBills.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateBillAsync_NeverSetsNitOrMinimumWage_ThoseAreImportOnly()
    {
        var result = await _sut.CreateBillAsync(ValidBrasilRequest());

        result.NitNumber.Should().BeNull();
        result.MinimumWageValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task CreateBillAsync_WithInvalidDueDay_Throws(int dueDay)
    {
        var request = ValidBrasilRequest();
        var invalidRequest = new RecurringBillCreateDTO
        {
            DueDay = dueDay,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note
        };

        var act = async () => await _sut.CreateBillAsync(invalidRequest);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBillAsync_WithBlankDescription_Throws()
    {
        var act = async () => await _sut.CreateBillAsync(new RecurringBillCreateDTO
        {
            DueDay = 10,
            Description = "   ",
            Value = 100m,
            Area = "Brasil",
            Note = string.Empty
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBillAsync_WithUnrecognizedArea_Throws()
    {
        var act = async () => await _sut.CreateBillAsync(new RecurringBillCreateDTO
        {
            DueDay = 10,
            Description = "Test",
            Value = 100m,
            Area = "France",
            Note = string.Empty
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteBillAsync_RemovesTheBill()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);
        var otherBill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        _repository.RecurringBills.Add(otherBill);

        await _sut.DeleteBillAsync(bill.Id);

        _repository.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(otherBill.Id);
    }

    [Fact]
    public async Task DeleteBillAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteBillAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void GetBills_ReturnsAllBills()
    {
        _repository.RecurringBills.Add(RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null));

        var result = _sut.GetBills();

        result.Should().ContainSingle(b => b.Description == "INSS");
    }

    private static RecurringBillUpdateDTO ValidUpdateRequest(string status = "Paid", decimal value = 900m) => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = value,
        Area = "Brasil",
        Note = string.Empty,
        NitNumber = null,
        MinimumWageValue = null,
        Status = status,
    };

    [Fact]
    public async Task UpdateBillAsync_UpdatesEveryField()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var result = await _sut.UpdateBillAsync(bill.Id, new RecurringBillUpdateDTO
        {
            DueDay = 15,
            Description = "INSS Renamed",
            Value = 900m,
            Area = "UK",
            Note = "Updated note",
            NitNumber = "12345678901",
            MinimumWageValue = 1621m,
            Status = "Paid",
        });

        using (new AssertionScope())
        {
            result.DueDay.Should().Be(15);
            result.Description.Should().Be("INSS Renamed");
            result.Value.Should().Be(900m);
            result.Area.Should().Be("UK");
            result.Note.Should().Be("Updated note");
            result.NitNumber.Should().Be("12345678901");
            result.MinimumWageValue.Should().Be(1621m);
            result.Status.Should().Be("Paid");
        }
    }

    [Fact]
    public async Task UpdateBillAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateBillAsync(Guid.NewGuid(), ValidUpdateRequest());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBillAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var act = async () => await _sut.UpdateBillAsync(bill.Id, ValidUpdateRequest(status: "NotAStatus"));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateBillAsync_WithInvalidArea_ThrowsArgumentException()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var act = async () => await _sut.UpdateBillAsync(bill.Id, new RecurringBillUpdateDTO
        {
            DueDay = 10,
            Description = "INSS",
            Value = 900m,
            Area = "NotAnArea",
            Note = string.Empty,
            Status = "Paid",
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task UpdateBillAsync_WithDueDayOutOfRange_ThrowsArgumentException(int dueDay)
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var act = async () => await _sut.UpdateBillAsync(bill.Id, new RecurringBillUpdateDTO
        {
            DueDay = dueDay,
            Description = "INSS",
            Value = 900m,
            Area = "Brasil",
            Note = string.Empty,
            Status = "Paid",
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateBillStatusAsync_WithValidStatus_UpdatesAndReturnsBill()
    {
        var bill = RecurringBill.Create(10, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var result = await _sut.UpdateBillStatusAsync(bill.Id, new RecurringBillStatusUpdateDTO { Status = "Paid" });

        using (new AssertionScope())
        {
            result.Status.Should().Be("Paid");
            _repository.RecurringBills.Should().ContainSingle().Which.Status.Should().Be(BillStatus.Paid);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateBillStatusAsync_DoesNotChangeOtherFields()
    {
        var bill = RecurringBill.Create(10, "Council Tax", 120m, Area.UK, "Direct debit", null, null);
        _repository.RecurringBills.Add(bill);

        var result = await _sut.UpdateBillStatusAsync(bill.Id, new RecurringBillStatusUpdateDTO { Status = "Paid" });

        using (new AssertionScope())
        {
            result.DueDay.Should().Be(10);
            result.Description.Should().Be("Council Tax");
            result.Value.Should().Be(120m);
            result.Area.Should().Be("UK");
            result.Note.Should().Be("Direct debit");
            result.NitNumber.Should().BeNull();
            result.MinimumWageValue.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateBillStatusAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateBillStatusAsync(Guid.NewGuid(), new RecurringBillStatusUpdateDTO { Status = "Paid" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBillStatusAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var bill = RecurringBill.Create(10, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        _repository.RecurringBills.Add(bill);

        var act = async () => await _sut.UpdateBillStatusAsync(bill.Id, new RecurringBillStatusUpdateDTO { Status = "NotAStatus" });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResetAllToUnsetAsync_SetsEveryBillStatusBackToUnset()
    {
        var paidBill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        paidBill.Update(10, "INSS", 850m, Area.Brasil, string.Empty, null, null, BillStatus.Paid);
        var scheduledBill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        scheduledBill.Update(15, "Council Tax", 120m, Area.UK, string.Empty, null, null, BillStatus.Scheduled);
        _repository.RecurringBills.Add(paidBill);
        _repository.RecurringBills.Add(scheduledBill);

        var result = await _sut.ResetAllToUnsetAsync();

        result.Should().OnlyContain(b => b.Status == "Unset");
        _repository.RecurringBills.Should().OnlyContain(b => b.Status == BillStatus.Unset);
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    private static RecurringBillCreateDTO ValidBrasilRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit"
    };

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new MensaisService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
