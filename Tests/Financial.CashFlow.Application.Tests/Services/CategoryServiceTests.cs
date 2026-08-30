using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CategoryServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CategoryService> Logger = NullLogger<CategoryService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultCategories: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private CategoryService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CategoryService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CategoryService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCategories_MapsEveryRepositoryCategoryToADto()
    {
        var result = _sut.GetCategories();

        result.Should().HaveCount(_repository.Categories.Count);
        result.Should().Contain(c => c.Name == "Mercado" && !c.IsInvestment && !c.IsTithe);
    }

    [Fact]
    public void GetCategories_RecordsSuccessfulSpan()
    {
        _sut.GetCategories();

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.CategoryService.GetCategories");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Category");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CategoryService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateCategoryAsync_WithValidRequest_AddsAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var sut = CreateService(repository);
        var request = new CategoryCreateDTO { Name = "Lazer", Active = true, IsInvestment = false, IsTithe = false };

        var result = await sut.CreateCategoryAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Lazer");
            result.Active.Should().BeTrue();
            result.HasReferences.Should().BeFalse();
            repository.Categories.Should().ContainSingle(c => c.Name == "Lazer");
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCategoryAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var repository = new StubCashFlowRepository();
        var sut = CreateService(repository);
        var request = new CategoryCreateDTO { Name = name!, Active = true, IsInvestment = false, IsTithe = false };

        var act = async () => await sut.CreateCategoryAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateCategoryAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        repository.Categories.Add(Category.Create("Mercado"));
        var sut = CreateService(repository);
        var request = new CategoryCreateDTO { Name = "Mercado", Active = true, IsInvestment = false, IsTithe = false };

        var act = async () => await sut.CreateCategoryAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithValidRequest_UpdatesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var category = Category.Create("Mercado");
        repository.Categories.Add(category);
        var sut = CreateService(repository);
        var request = new CategoryUpdateDTO { Name = "Casa", Active = false, IsInvestment = true, IsTithe = true };

        var result = await sut.UpdateCategoryAsync(category.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Casa");
            result.Active.Should().BeFalse();
            result.IsInvestment.Should().BeTrue();
            result.IsTithe.Should().BeTrue();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateCategoryAsync_ToItsOwnCurrentName_Succeeds()
    {
        var repository = new StubCashFlowRepository();
        var category = Category.Create("Mercado");
        repository.Categories.Add(category);
        var sut = CreateService(repository);
        var request = new CategoryUpdateDTO { Name = "Mercado", Active = false, IsInvestment = false, IsTithe = false };

        var result = await sut.UpdateCategoryAsync(category.Id, request);

        result.Active.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository();
        var sut = CreateService(repository);
        var request = new CategoryUpdateDTO { Name = "Casa", Active = true, IsInvestment = false, IsTithe = false };

        var act = async () => await sut.UpdateCategoryAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        var mercado = Category.Create("Mercado");
        var casa = Category.Create("Casa");
        repository.Categories.Add(mercado);
        repository.Categories.Add(casa);
        var sut = CreateService(repository);
        var request = new CategoryUpdateDTO { Name = "Casa", Active = true, IsInvestment = false, IsTithe = false };

        var act = async () => await sut.UpdateCategoryAsync(mercado.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithNoReferences_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var category = Category.Create("Mercado");
        repository.Categories.Add(category);
        var sut = CreateService(repository);

        await sut.DeleteCategoryAsync(category.Id);

        using (new AssertionScope())
        {
            repository.Categories.Should().BeEmpty();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository();
        var sut = CreateService(repository);

        var act = async () => await sut.DeleteCategoryAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReferencedByExpense_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        var category = Category.Create("Mercado");
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        repository.Categories.Add(category);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, category, bank, null));
        var sut = CreateService(repository);

        var act = async () => await sut.DeleteCategoryAsync(category.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            repository.Categories.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void GetCategories_WhenReferencedByExpense_HasReferencesIsTrue()
    {
        var repository = new StubCashFlowRepository();
        var category = Category.Create("Mercado");
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        repository.Categories.Add(category);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, category, bank, null));
        var sut = CreateService(repository);

        var result = sut.GetCategories();

        result.Should().ContainSingle(c => c.Name == "Mercado" && c.HasReferences);
    }
}
