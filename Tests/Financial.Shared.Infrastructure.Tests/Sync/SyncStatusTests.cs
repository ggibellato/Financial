using Financial.Shared.Abstractions.Sync;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Sync;

public class SyncStatusTests
{
    [Fact]
    public void SyncState_Should_Have_Exactly_Four_Members()
    {
        var members = Enum.GetNames<SyncState>();

        members.Should().BeEquivalentTo(["Idle", "Pending", "Saving", "Failed"]);
    }
}
