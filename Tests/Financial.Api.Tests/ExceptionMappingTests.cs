using System.Net;
using System.Reflection;
using Financial.CashFlow.Application.Exceptions;
using Financial.Investment.Application.Exceptions;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions.Resilience;
using FluentAssertions;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ExceptionMappingTests(ThrowingApiHost host) : IClassFixture<ThrowingApiHost>
{
    private static readonly (Exception Exception, HttpStatusCode Status)[] MappedExceptions =
    [
        (new OverdraftConfirmationRequiredException("overdraft"), HttpStatusCode.Conflict),
        (new ReserveMovementLinkedToIncomeException("linked"), HttpStatusCode.Conflict),
        (new EntityInUseException("in use"), HttpStatusCode.Conflict),
        (new DuplicateNameException("duplicate"), HttpStatusCode.Conflict),
        (new InvestmentRuleViolationException("rule"), HttpStatusCode.Conflict),
        (new UnsupportedAssetClassException("unsupported"), HttpStatusCode.UnprocessableEntity),
        (new KeyNotFoundException("missing"), HttpStatusCode.NotFound),
        (new DividendNotFoundException("missing"), HttpStatusCode.NotFound),
        (new ArgumentException("invalid"), HttpStatusCode.BadRequest),
        (new TransientStorageException("transient", new InvalidOperationException()), HttpStatusCode.ServiceUnavailable)
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
        var (exception, status) = MappedExceptions[index];

        var response = await host.GetBanksFailingWith(exception);

        response.StatusCode.Should().Be(status, because: exception.GetType().Name);
        var retryAfter = response.Headers.RetryAfter?.Delta;
        retryAfter.Should().Be(status == HttpStatusCode.ServiceUnavailable ? TimeSpan.FromSeconds(30) : null);
    }

    [Fact]
    public void EveryExceptionTypeInTheApplicationAssemblies_IsMappedOrExempt()
    {
        var candidates = ScannedAssemblies
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .SelectMany(assembly => assembly.GetTypes())
            .ToList();
        var mapped = MappedExceptions.Select(row => row.Exception.GetType());

        var unmapped = FindUnmappedExceptionTypes(candidates, mapped, HandledInTheServiceLayer);

        candidates.Should().Contain(typeof(DuplicateNameException));
        unmapped.Should().BeEmpty(
            "every exception that can reach HTTP must be mapped by DomainExceptionMappingMiddleware and listed in MappedExceptions, or be handled in the service layer");
    }

    [Fact]
    public void Scanner_FlagsUnmappedTypes_AndAcceptsMappedOrExemptOnes()
    {
        Type[] candidates = [typeof(SyntheticUnmappedException), typeof(SyntheticMappedException), typeof(CalendarNotFoundException), typeof(string)];

        var unmapped = FindUnmappedExceptionTypes(candidates, [typeof(SyntheticMappedException)], [typeof(CalendarNotFoundException)]);

        unmapped.Should().ContainSingle().Which.Should().Contain(nameof(SyntheticUnmappedException));
    }

    [Fact]
    public void Scanner_AcceptsASubclassOfAMappedType()
    {
        var unmapped = FindUnmappedExceptionTypes([typeof(SyntheticMappedSubclass)], [typeof(SyntheticMappedException)], []);

        unmapped.Should().BeEmpty();
    }

    private static List<string> FindUnmappedExceptionTypes(IEnumerable<Type> candidates, IEnumerable<Type> mapped, IEnumerable<Type> exempt)
    {
        var handled = mapped.Concat(exempt).ToList();
        return candidates
            .Where(type => typeof(Exception).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => !handled.Any(handledType => handledType.IsAssignableFrom(type)))
            .Select(type => type.FullName ?? type.Name)
            .ToList();
    }

    private class SyntheticMappedException : Exception;

    private sealed class SyntheticMappedSubclass : SyntheticMappedException;

    private sealed class SyntheticUnmappedException : Exception;
}
