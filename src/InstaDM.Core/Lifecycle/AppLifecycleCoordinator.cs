namespace InstaDM.Core.Lifecycle;

/// <summary>
/// Tracks app-owned background work that must die with the last window.
/// No tray, no startup task, no service — closing the window ends the app.
/// </summary>
public sealed class AppLifecycleCoordinator
{
    private readonly List<IDisposable> _owned = [];
    private readonly object _gate = new();
    private bool _shuttingDown;

    public bool IsShuttingDown
    {
        get { lock (_gate) { return _shuttingDown; } }
    }

    /// <summary>Register a disposable (poller, watcher, timer). Disposed on
    /// <see cref="Shutdown"/>.</summary>
    public void Own(IDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        lock (_gate)
        {
            if (_shuttingDown)
            {
                resource.Dispose();
                return;
            }

            _owned.Add(resource);
        }
    }

    /// <summary>Idempotent shutdown of every owned resource.</summary>
    public void Shutdown()
    {
        List<IDisposable> snapshot;
        lock (_gate)
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            snapshot = [.. _owned];
            _owned.Clear();
        }

        foreach (var resource in snapshot)
        {
            try { resource.Dispose(); }
            catch { /* never block exit */ }
        }
    }
}
