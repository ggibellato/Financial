using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Unit")]
public class CashFlowDataFileVersionTests
{
    [Fact]
    public void StoredInAndStamp_NullRoot_Throw()
    {
        using var _ = new AssertionScope();
        FluentActions.Invoking(() => CashFlowDataFileVersion.StoredIn((JsonObject)null!))
            .Should().Throw<ArgumentNullException>().WithParameterName("root");
        FluentActions.Invoking(() => CashFlowDataFileVersion.Stamp(null!, CashFlowDataFileVersion.Current))
            .Should().Throw<ArgumentNullException>().WithParameterName("root");
    }
}
