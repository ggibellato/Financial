using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class ViewModelBaseTests
{
    private sealed class TestViewModel : ViewModelBase
    {
        public Task Refresh(
            Func<int> beginRequest, Func<int, bool> isCurrentRequest,
            Action<bool> setLoading, Action<string?> setError, Func<Func<bool>, Task> refresh,
            Action<Exception>? logError = null) =>
            ExecuteRefreshAsync(beginRequest, isCurrentRequest, setLoading, setError, refresh, logError);
    }

    [Fact]
    public async Task Refresh_HappyPath_TogglesLoadingAndClearsError()
    {
        var vm = new TestViewModel();
        var requestId = 0;
        var loadingStates = new List<bool>();
        string? error = "stale error";
        var applied = false;

        await vm.Refresh(
            () => ++requestId,
            id => id == requestId,
            loading => loadingStates.Add(loading),
            e => error = e,
            async isCurrent =>
            {
                await Task.Yield();
                if (isCurrent())
                {
                    applied = true;
                }
            });

        loadingStates.Should().Equal(true, false);
        error.Should().BeNull();
        applied.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_BodyThrows_SetsErrorAndClearsLoading()
    {
        var vm = new TestViewModel();
        var requestId = 0;
        bool? loading = null;
        string? error = null;

        await vm.Refresh(
            () => ++requestId,
            id => id == requestId,
            l => loading = l,
            e => error = e,
            _ => throw new InvalidOperationException("boom"));

        loading.Should().BeFalse();
        error.Should().Be("boom");
    }

    [Fact]
    public async Task Refresh_BodyThrows_InvokesLogErrorWithException()
    {
        var vm = new TestViewModel();
        var requestId = 0;
        Exception? logged = null;

        await vm.Refresh(
            () => ++requestId,
            id => id == requestId,
            _ => { },
            _ => { },
            _ => throw new InvalidOperationException("boom"),
            ex => logged = ex);

        logged.Should().NotBeNull();
        logged!.Message.Should().Be("boom");
    }

    [Fact]
    public async Task Refresh_OverlappingCalls_StaleCompletionDiscardsItsOwnResultsAndDoesNotClobberNewerState()
    {
        var vm = new TestViewModel();
        var requestId = 0;
        var loadingStates = new List<bool>();
        string? error = null;
        var appliedByFirst = false;
        var appliedBySecond = false;
        var firstGate = new TaskCompletionSource();

        var firstCall = vm.Refresh(
            () => ++requestId,
            id => id == requestId,
            loading => loadingStates.Add(loading),
            e => error = e,
            async isCurrent =>
            {
                await firstGate.Task;
                if (isCurrent())
                {
                    appliedByFirst = true;
                }
            });

        await vm.Refresh(
            () => ++requestId,
            id => id == requestId,
            loading => loadingStates.Add(loading),
            e => error = e,
            async isCurrent =>
            {
                await Task.Yield();
                if (isCurrent())
                {
                    appliedBySecond = true;
                }
            });

        appliedBySecond.Should().BeTrue();
        loadingStates.Last().Should().BeFalse();

        firstGate.SetResult();
        await firstCall;

        appliedByFirst.Should().BeFalse();
        error.Should().BeNull();
        loadingStates.Last().Should().BeFalse();
    }
}
