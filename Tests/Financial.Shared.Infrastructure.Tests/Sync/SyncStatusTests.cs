using Financial.Shared.Infrastructure.Sync;
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

    [Fact]
    public void SyncStatus_Should_Expose_State_Error_And_Timestamp()
    {
        var timestamp = new DateTime(2026, 8, 13, 10, 30, 0, DateTimeKind.Utc);

        var status = new SyncStatus(SyncState.Failed, "Drive upload failed", timestamp);

        status.State.Should().Be(SyncState.Failed);
        status.LastError.Should().Be("Drive upload failed");
        status.LastSuccessfulSaveUtc.Should().Be(timestamp);
    }
}
