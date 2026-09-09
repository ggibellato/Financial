using System.Collections.Concurrent;

namespace Financial.Presentation.Tests.Input;

/// <summary>WPF controls require STA thread affinity (InputManager/KeyboardNavigation), which the
/// default xUnit test thread does not provide. This fixture keeps one STA thread alive for the whole
/// test class instead of spinning up a new OS thread per test.</summary>
public sealed class StaThreadFixture : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public StaThreadFixture()
    {
        _thread = new Thread(() =>
        {
            foreach (var action in _queue.GetConsumingEnumerable())
            {
                action();
            }
        });
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Run(Action action)
    {
        Exception? failure = null;
        using var done = new ManualResetEventSlim(false);

        _queue.Add(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                done.Set();
            }
        });

        done.Wait();

        if (failure != null)
        {
            throw failure;
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
    }
}
