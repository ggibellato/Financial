using System.Runtime.CompilerServices;

namespace Financial.Presentation.Tests;

/// <summary>
/// Several ViewModel tests exercise fire-and-forget Task.Run calls and then wait on the
/// result with a short timeout (a few seconds). .NET's default ThreadPool minimum worker
/// count equals Environment.ProcessorCount, and new threads beyond that are injected at a
/// throttled rate. On a 2-core CI runner (GitHub-hosted windows-latest), that default is
/// too low for several such tests to run without contention, causing intermittent
/// TimeoutException failures that don't reproduce on a higher-core-count machine.
/// Raising the minimum up front removes the throttled ramp-up delay entirely.
/// </summary>
internal static class ThreadPoolWarmup
{
    [ModuleInitializer]
    internal static void EnsureSufficientMinThreads()
    {
        ThreadPool.GetMinThreads(out var currentWorkerThreads, out var currentCompletionPortThreads);
        ThreadPool.SetMinThreads(Math.Max(currentWorkerThreads, 32), Math.Max(currentCompletionPortThreads, 32));
    }
}
