using FluentAssertions;

namespace Financial.Api.Tests;

[Trait("Category", "Unit")]
public class SnapshotUpdateFlagTests
{
    private static Func<string, string?> Environment(string? flag, string? ci) =>
        name => name switch
        {
            SnapshotUpdateFlag.Name => flag,
            "CI" => ci,
            _ => null
        };

    [Fact]
    public void IsRequested_WithFlagOnCi_FailsWithTheGuardMessage()
    {
        var act = () => SnapshotUpdateFlag.IsRequested(Environment(flag: "1", ci: "true"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("UPDATE_OPENAPI_SNAPSHOT must not be set in CI");
    }

    [Fact]
    public void IsRequested_WithFlagOffCi_AllowsTheUpdate()
    {
        SnapshotUpdateFlag.IsRequested(Environment(flag: "1", ci: null)).Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "true")]
    [InlineData(null, "true")]
    public void IsRequested_WithoutFlag_IsNotRequestedEvenOnCi(string? flag, string? ci)
    {
        SnapshotUpdateFlag.IsRequested(Environment(flag, ci)).Should().BeFalse();
    }
}
