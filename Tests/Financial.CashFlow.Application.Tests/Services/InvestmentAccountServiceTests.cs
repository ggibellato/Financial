using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class InvestmentAccountServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentAccountService> Logger = NullLogger<InvestmentAccountService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly InvestmentAccountService _sut;

    public InvestmentAccountServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private InvestmentAccountService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void GetInvestmentAccounts_MapsEveryRepositoryAccountToADto()
    {
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var platinumVisa = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        _repository.InvestmentAccounts.Add(chaseSave);
        _repository.InvestmentAccounts.Add(platinumVisa);

        var result = _sut.GetInvestmentAccounts();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var chaseSaveDto = result.Should().ContainSingle(a => a.Name == "ChaseSave").Which;
            chaseSaveDto.Id.Should().Be(chaseSave.Id);
            chaseSaveDto.IsActive.Should().BeTrue();
            chaseSaveDto.IsLiability.Should().BeFalse();
            var platinumVisaDto = result.Should().ContainSingle(a => a.Name == "PlatinumVisa8003").Which;
            platinumVisaDto.IsLiability.Should().BeTrue();
        }
    }

    [Fact]
    public void GetInvestmentAccounts_DoesNotFilterByIsActive()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("RetiredAccount", isActive: false, isLiability: false));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Name == "RetiredAccount" && !a.IsActive);
    }

    [Fact]
    public void GetInvestmentAccounts_WithNoAccounts_ReturnsEmptyList()
    {
        var result = _sut.GetInvestmentAccounts();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetInvestmentAccounts_WithNoSnapshots_HasNonZeroInvestmentSnapshotIsFalse()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Id == account.Id).Which.HasNonZeroInvestmentSnapshot.Should().BeFalse();
    }

    [Fact]
    public void GetInvestmentAccounts_WithOnlyZeroValueSnapshots_HasNonZeroInvestmentSnapshotIsFalse()
    {
        // A zero-valued snapshot (e.g. an import-generated placeholder for a not-yet-happened month)
        // must not read as "has a balance" - only an actual non-zero recorded value should.
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 0m));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Id == account.Id).Which.HasNonZeroInvestmentSnapshot.Should().BeFalse();
    }

    [Fact]
    public void GetInvestmentAccounts_WithANonZeroSnapshot_HasNonZeroInvestmentSnapshotIsTrue()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 500m));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Id == account.Id).Which.HasNonZeroInvestmentSnapshot.Should().BeTrue();
    }

    [Fact]
    public void GetInvestmentAccounts_WithANonZeroSnapshotInAnyMonth_HasNonZeroInvestmentSnapshotIsTrueRegardlessOfOrder()
    {
        // Reproduces the original bug scenario: a real balance recorded mid-year, followed by
        // import-generated zero placeholders for later, not-yet-happened months. The old
        // "chronologically latest" check would miss this; checking every snapshot does not.
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 9, 5000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 10, 0m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 11, 0m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 12, 0m));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Id == account.Id).Which.HasNonZeroInvestmentSnapshot.Should().BeTrue();
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_WithValidRequest_AddsAndSaves()
    {
        var request = new InvestmentAccountCreateDTO { Name = "ChaseSave", IsActive = true, IsLiability = false };

        var result = await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("ChaseSave");
            result.HasNonZeroInvestmentSnapshot.Should().BeFalse();
            _repository.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateInvestmentAccountAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var request = new InvestmentAccountCreateDTO { Name = name!, IsActive = true, IsLiability = false };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));
        var request = new InvestmentAccountCreateDTO { Name = "ChaseSave", IsActive = true, IsLiability = false };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithValidRequest_UpdatesAndSaves()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        var request = new InvestmentAccountUpdateDTO { Name = "ChaseSaveRenamed", IsActive = false, IsLiability = true };

        var result = await _sut.UpdateInvestmentAccountAsync(account.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("ChaseSaveRenamed");
            result.IsActive.Should().BeFalse();
            result.IsLiability.Should().BeTrue();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var request = new InvestmentAccountUpdateDTO { Name = "X", IsActive = true, IsLiability = false };

        var act = async () => await _sut.UpdateInvestmentAccountAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var baAmex = InvestmentAccount.Create("BaAmex", isActive: true, isLiability: true);
        _repository.InvestmentAccounts.Add(chaseSave);
        _repository.InvestmentAccounts.Add(baAmex);
        var request = new InvestmentAccountUpdateDTO { Name = "BaAmex", IsActive = true, IsLiability = false };

        var act = async () => await _sut.UpdateInvestmentAccountAsync(chaseSave.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithNoSnapshots_RemovesAndSaves()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);

        await _sut.DeleteInvestmentAccountAsync(account.Id);

        using (new AssertionScope())
        {
            _repository.InvestmentAccounts.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithOnlyZeroValueSnapshots_RemovesAndSaves()
    {
        // Zero-valued snapshots (e.g. import-generated placeholders for not-yet-happened months, or
        // simply opening the Investment Snapshots page for the current month, which auto-creates one
        // per active account) must not block deletion - only a genuinely recorded non-zero value should.
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 0m));

        await _sut.DeleteInvestmentAccountAsync(account.Id);

        _repository.InvestmentAccounts.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteInvestmentAccountAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithNonZeroSnapshot_ThrowsAndWritesNothing()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 500m));

        var act = async () => await _sut.DeleteInvestmentAccountAsync(account.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.InvestmentAccounts.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithNonZeroSnapshotInAnyMonth_ThrowsRegardlessOfOrder()
    {
        // Reproduces the original bug scenario: a real balance recorded mid-year, followed by
        // import-generated zero placeholders for later, not-yet-happened months. The old
        // "chronologically latest" check would have missed this and allowed the delete.
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 9, 5000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 10, 0m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 11, 0m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 12, 0m));

        var act = async () => await _sut.DeleteInvestmentAccountAsync(account.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_SourceCreditCardWithUnresolvedId_Throws()
    {
        var request = new InvestmentAccountCreateDTO
        {
            Name = "PlatinumVisa8003",
            IsActive = true,
            IsLiability = true,
            Source = "CreditCard",
            CreditCardId = Guid.NewGuid()
        };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_SourceCreditCardWithInactiveCard_Throws()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: false);
        _repository.Accounts.Add(creditCard);
        var request = new InvestmentAccountCreateDTO
        {
            Name = "PlatinumVisa8003",
            IsActive = true,
            IsLiability = true,
            Source = "CreditCard",
            CreditCardId = creditCard.Id
        };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_SourceCreditCardWithActiveCard_Succeeds()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);
        _repository.Accounts.Add(creditCard);
        var request = new InvestmentAccountCreateDTO
        {
            Name = "PlatinumVisa8003",
            IsActive = true,
            IsLiability = true,
            Source = "CreditCard",
            CreditCardId = creditCard.Id
        };

        var result = await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            result.Source.Should().Be("CreditCard");
            result.CreditCardId.Should().Be(creditCard.Id);
        }
    }

    private static InvestmentAccountUpdateDTO PotsSourced(string name, Guid? potsAccountId) => new()
    {
        Name = name,
        IsActive = true,
        IsLiability = false,
        Source = "PotsAccountTotal",
        PotsAccountId = potsAccountId
    };

    [Fact]
    public async Task CreateInvestmentAccountAsync_SourcePotsAccountTotal_NamesTheChosenPotsAccount()
    {
        _repository.EnsureReserva();
        var holidays = TestAccounts.Pots("Holidays");
        _repository.Accounts.Add(holidays);

        var result = await _sut.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
        {
            Name = "Holiday savings",
            IsActive = true,
            IsLiability = false,
            Source = "PotsAccountTotal",
            PotsAccountId = holidays.Id
        });

        using (new AssertionScope())
        {
            var created = _repository.InvestmentAccounts.Should().ContainSingle(account => account.Id == result.Id).Which;
            created.Source.Should().Be(InvestmentAccountSource.PotsAccountTotal);
            created.PotsAccount.Should().BeSameAs(holidays);
            result.Source.Should().Be("PotsAccountTotal");
            result.PotsAccountId.Should().Be(holidays.Id);
            result.CreditCardId.Should().BeNull();
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_SourcePotsAccountTotalWithoutAPotsAccount_ThrowsAndWritesNothing()
    {
        _repository.EnsureReserva();

        var act = () => _sut.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
        {
            Name = "Reservas pessoais",
            IsActive = true,
            IsLiability = false,
            Source = "PotsAccountTotal"
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("A pots-account-sourced investment account requires a Pots account.");
            _repository.InvestmentAccounts.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.InvestmentAccountService.CreateInvestmentAccount");
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_UnknownPotsAccountId_ThrowsAndWritesNothing()
    {
        var unknownId = Guid.NewGuid();

        var act = () => _sut.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
        {
            Name = "Reservas pessoais",
            IsActive = true,
            IsLiability = false,
            Source = "PotsAccountTotal",
            PotsAccountId = unknownId
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Pots account '{unknownId}' is not recognized.");
            _repository.InvestmentAccounts.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.InvestmentAccountService.CreateInvestmentAccount");
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_NamingABankAsThePotsAccount_Throws()
    {
        var barclays = TestAccounts.Bank("Barclays");
        _repository.Accounts.Add(barclays);

        var act = () => _sut.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
        {
            Name = "Reservas pessoais",
            IsActive = true,
            IsLiability = false,
            Source = "PotsAccountTotal",
            PotsAccountId = barclays.Id
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Barclays is not a Pots account.");
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.InvestmentAccountService.CreateInvestmentAccount");
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_LegacySourceReserveBucketsSum_IsNoLongerAccepted()
    {
        _repository.EnsureReserva();

        var act = () => _sut.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
        {
            Name = "Reservas pessoais",
            IsActive = true,
            IsLiability = false,
            Source = "ReserveBucketsSum"
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Source 'ReserveBucketsSum' is not recognized.");
            _repository.InvestmentAccounts.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.InvestmentAccountService.CreateInvestmentAccount");
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_ItsPotsAccountDeactivatedSinceLinking_KeepsTheLink()
    {
        var reserva = _repository.EnsureReserva();
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);
        _repository.InvestmentAccounts.Add(account);
        reserva.SetActive(false);

        await _sut.UpdateInvestmentAccountAsync(account.Id, PotsSourced("Reservas", reserva.Id));

        using (new AssertionScope())
        {
            account.Name.Should().Be("Reservas");
            account.PotsAccount.Should().BeSameAs(reserva);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_ChangingToAnInactivePotsAccount_ThrowsAndKeepsTheLink()
    {
        var reserva = _repository.EnsureReserva();
        var holidays = TestAccounts.Pots("Holidays", isActive: false);
        _repository.Accounts.Add(holidays);
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);
        _repository.InvestmentAccounts.Add(account);

        var act = () => _sut.UpdateInvestmentAccountAsync(account.Id, PotsSourced("Reservas pessoais", holidays.Id));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Holidays is inactive.");
            account.PotsAccount.Should().BeSameAs(reserva);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.InvestmentAccountService.UpdateInvestmentAccount");
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_SwitchingToAnotherPotsAccount_NamesIt()
    {
        var reserva = _repository.EnsureReserva();
        var holidays = TestAccounts.Pots("Holidays");
        _repository.Accounts.Add(holidays);
        var account = InvestmentAccount.Create(
            "Savings", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);
        _repository.InvestmentAccounts.Add(account);

        var result = await _sut.UpdateInvestmentAccountAsync(account.Id, PotsSourced("Savings", holidays.Id));

        using (new AssertionScope())
        {
            account.PotsAccount.Should().BeSameAs(holidays);
            result.PotsAccountId.Should().Be(holidays.Id);
        }
    }

    [Fact]
    public async Task GetInvestmentAccounts_MapsSourceAndCreditCardId()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);
        _repository.Accounts.Add(creditCard);
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard));

        var result = _sut.GetInvestmentAccounts();

        var dto = result.Should().ContainSingle(a => a.Name == "PlatinumVisa8003").Which;
        using (new AssertionScope())
        {
            dto.Source.Should().Be("CreditCard");
            dto.CreditCardId.Should().Be(creditCard.Id);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_SwitchingAwayFromCreditCard_ClearsCreditCardIdRegardlessOfRequestValue()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);
        _repository.Accounts.Add(creditCard);
        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);
        _repository.InvestmentAccounts.Add(account);
        var request = new InvestmentAccountUpdateDTO
        {
            Name = "PlatinumVisa8003",
            IsActive = true,
            IsLiability = true,
            Source = "None",
            CreditCardId = creditCard.Id
        };

        var result = await _sut.UpdateInvestmentAccountAsync(account.Id, request);

        using (new AssertionScope())
        {
            result.Source.Should().Be("None");
            result.CreditCardId.Should().BeNull();
        }
    }

    [Fact]
    public void GetInvestmentAccounts_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetInvestmentAccounts();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.InvestmentAccountService.GetInvestmentAccounts");
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithBlankName_ThrowsArgumentException()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        var request = new InvestmentAccountUpdateDTO { Name = "   ", IsActive = true, IsLiability = false };

        var act = async () => await _sut.UpdateInvestmentAccountAsync(account.Id, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*name is required*");
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_WithUnrecognizedSource_ThrowsArgumentException()
    {
        var request = new InvestmentAccountCreateDTO { Name = "New Account", IsActive = true, IsLiability = false, Source = "NotARealSource" };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }
}
