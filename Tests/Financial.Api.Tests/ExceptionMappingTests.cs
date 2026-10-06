using System.Net;
using System.Reflection;
using Financial.CashFlow.Application.Exceptions;
using Financial.Investment.Application.Exceptions;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions.Resilience;
using FluentAssertions;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ExceptionMappingTests
{
    private const string BanksPath = "/api/v1/financial/banks";

    private static readonly (Func<Exception> Create, HttpStatusCode Status)[] MappedExceptions =
    [
        (() => new OverdraftConfirmationRequiredException("overdraft"), HttpStatusCode.Conflict),
        (() => new ReserveMovementLinkedToIncomeException("linked"), HttpStatusCode.Conflict),
        (() => new EntityInUseException("in use"), HttpStatusCode.Conflict),
        (() => new DuplicateNameException("duplicate"), HttpStatusCode.Conflict),
        (() => new InvestmentRuleViolationException("rule"), HttpStatusCode.Conflict),
        (() => new UnsupportedAssetClassException("unsupported"), HttpStatusCode.UnprocessableEntity),
        (() => new KeyNotFoundException("missing"), HttpStatusCode.NotFound),
        (() => new DividendNotFoundException("missing"), HttpStatusCode.NotFound),
        (() => new ArgumentException("invalid"), HttpStatusCode.BadRequest),
        (() => new TransientStorageException("transient", new InvalidOperationException()), HttpStatusCode.ServiceUnavailable)
    ];

    private static readonly Type[] HandledInTheServiceLayer =
    [
        typeof(CalendarNotFoundException),
        typeof(CalendarTokenRevokedException)
    ];

    private static readonly string[] ScannedAssemblies =
    [
        "Financial.CashFlow.Domain",
        "Financial.CashFlow.Application",
        "Financial.Investment.Domain",
        "Financial.Investment.Application",
        "Financial.Shared.Abstractions"
    ];

    public static TheoryData<int> MappedExceptionIndexes() => [.. Enumerable.Range(0, MappedExceptions.Length)];

    [Theory]
    [MemberData(nameof(MappedExceptionIndexes))]
    public async Task MappedException_ReturnsItsStatusCode(int index)
    {
        var (create, status) = MappedExceptions[index];
        await using var factory = new ApiTestFactory(configureServices: ThrowingBankService.Registering(create));
        using var client = factory.CreateClient();

        var response = await client.GetAsync(BanksPath);

        response.StatusCode.Should().Be(status, because: create().GetType().Name);
        var retryAfter = response.Headers.RetryAfter?.Delta;
        retryAfter.Should().Be(status == HttpStatusCode.ServiceUnavailable ? TimeSpan.FromSeconds(30) : null);
    }

    [Fact]
    public void EveryExceptionTypeInTheApplicationAssemblies_IsMappedOrExempt()
    {
        var candidates = ScannedAssemblies
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .SelectMany(assembly => assembly.GetTypes());
        var mapped = MappedExceptions.Select(row => row.Create().GetType()).ToList();

        var unmapped = FindUnmappedExceptionTypes(candidates, mapped, HandledInTheServiceLayer);

        unmapped.Should().BeEmpty(
            "every exception that can reach HTTP must be mapped by DomainExceptionMappingMiddleware and listed in MappedExceptions, or be handled in the service layer");
    }

    [Fact]
    public void Scanner_FlagsAnUnmappedExceptionType()
    {
        var unmapped = FindUnmappedExceptionTypes([typeof(SyntheticUnmappedException), typeof(string)], new HashSet<Type>(), Array.Empty<Type>());

        unmapped.Should().ContainSingle().Which.Should().Contain(nameof(SyntheticUnmappedException));
    }

    [Fact]
    public void Scanner_AcceptsMappedAndExemptTypes()
    {
        var unmapped = FindUnmappedExceptionTypes(
            [typeof(SyntheticUnmappedException), typeof(CalendarNotFoundException)],
            new[] { typeof(SyntheticUnmappedException) },
            new[] { typeof(CalendarNotFoundException) });

        unmapped.Should().BeEmpty();
    }

    private static List<string> FindUnmappedExceptionTypes(IEnumerable<Type> candidates, IReadOnlyCollection<Type> mapped, IEnumerable<Type> exempt) =>
        candidates
            .Where(type => typeof(Exception).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => !mapped.Contains(type) && !exempt.Contains(type))
            .Select(type => type.FullName ?? type.Name)
            .ToList();

    private sealed class SyntheticUnmappedException : Exception;
}
