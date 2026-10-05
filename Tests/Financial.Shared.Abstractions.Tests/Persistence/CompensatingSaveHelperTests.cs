using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;

namespace Financial.Shared.Abstractions.Tests.Persistence;

[Trait("Category", "Unit")]
public class CompensatingSaveHelperTests
{
    [Fact]
    public async Task ApplyWithCompensationAsync_ApplySucceeds_DoesNotRunRollback()
    {
        var calls = new List<string>();

        await CompensatingSaveHelper.ApplyWithCompensationAsync(
            () => { calls.Add("apply"); return Task.FromResult(true); },
            () => { calls.Add("rollback"); return Task.FromResult(false); });

        calls.Should().Equal("apply");
    }

    [Fact]
    public async Task ApplyWithCompensationAsync_ApplyThrows_RunsRollbackThenRethrowsTheOriginalException()
    {
        var calls = new List<string>();
        var failure = new InvalidOperationException("save failed");

        var act = () => CompensatingSaveHelper.ApplyWithCompensationAsync(
            () => { calls.Add("apply"); throw failure; },
            () => { calls.Add("rollback"); return Task.FromResult(false); });

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        calls.Should().Equal("apply", "rollback");
    }

    [Fact]
    public async Task ApplyWithCompensationAsync_ApplyFailsAsynchronously_RunsRollbackThenRethrows()
    {
        var rolledBack = false;

        var act = () => CompensatingSaveHelper.ApplyWithCompensationAsync(
            async () => { await Task.Yield(); throw new IOException("disk full"); },
            () => { rolledBack = true; return Task.FromResult(false); });

        await act.Should().ThrowAsync<IOException>().WithMessage("disk full");
        rolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyWithCompensationAsync_RollbackThrows_PropagatesTheRollbackFailure()
    {
        var act = () => CompensatingSaveHelper.ApplyWithCompensationAsync(
            () => throw new InvalidOperationException("save failed"),
            () => throw new TimeoutException("rollback failed"));

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("rollback failed");
    }
}
