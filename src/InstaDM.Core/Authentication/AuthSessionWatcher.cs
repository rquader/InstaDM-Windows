namespace InstaDM.Core.Authentication;

/// <summary>
/// Privacy-safe probe: reports whether the Instagram session cookie EXISTS.
/// Implementations must never read, retain, log, or expose the value. The
/// WinUI adapter uses CoreWebView2.CookieManager.GetCookiesAsync and checks
/// only for the presence of the session cookie name; tests use fakes.
/// </summary>
public interface ISessionCookieProbe
{
    Task<bool> SessionCookieExistsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Bounded cookie-presence poller. The macOS app's hard-won auth model:
/// don't guess redirects — poll for the session cookie and let its
/// existence drive the state machine (docs/SOURCE_BEHAVIOR.md B6).
///
/// Guarantees:
/// <list type="bullet">
/// <item>Single flight: starting an already-running watcher is a no-op
/// (duplicate-timer races were a real macOS defect class).</item>
/// <item>Backoff: starts fast (login completes in seconds), decays to the
/// idle interval so a stalled login page isn't hammered.</item>
/// <item>Cancellation: stopping is immediate and idempotent; no callback
/// fires after Stop.</item>
/// <item>Probe faults are treated as "absent" (fail closed) and do not
/// kill the loop.</item>
/// </list>
/// </summary>
public sealed class AuthSessionWatcher : IDisposable
{
    public static readonly TimeSpan InitialInterval = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(10);
    private const double BackoffFactor = 1.5;

    private readonly ISessionCookieProbe _probe;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;

    /// <param name="probe">Cookie-existence adapter.</param>
    /// <param name="delay">Injectable for tests; defaults to Task.Delay.</param>
    public AuthSessionWatcher(
        ISessionCookieProbe probe,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _probe = probe;
        _delay = delay ?? Task.Delay;
    }

    public bool IsRunning
    {
        get { lock (_gate) { return _cts is not null; } }
    }

    /// <summary>Starts polling. Each observation is delivered to
    /// <paramref name="onObservation"/> (true = cookie exists). Returns
    /// false if already running (no duplicate loops, ever).</summary>
    public bool Start(Action<bool> onObservation)
    {
        CancellationTokenSource cts;
        lock (_gate)
        {
            if (_cts is not null)
            {
                return false;
            }
            cts = new CancellationTokenSource();
            _cts = cts;
        }

        _ = RunAsync(onObservation, cts.Token);
        return true;
    }

    /// <summary>Stops polling. Idempotent; safe from any thread.</summary>
    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
        }
        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void Dispose() => Stop();

    private async Task RunAsync(Action<bool> onObservation, CancellationToken token)
    {
        // Guarantee Start returns immediately even if probe and delay both
        // complete synchronously (as fakes do in tests).
        await Task.Yield();

        var interval = InitialInterval;
        try
        {
            while (!token.IsCancellationRequested)
            {
                bool exists;
                try
                {
                    exists = await _probe.SessionCookieExistsAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // Probe fault: fail closed, keep polling.
                    exists = false;
                }

                if (token.IsCancellationRequested)
                {
                    return; // never observe after Stop
                }

                onObservation(exists);

                await _delay(interval, token).ConfigureAwait(false);
                var nextTicks = (long)(interval.Ticks * BackoffFactor);
                interval = TimeSpan.FromTicks(Math.Min(nextTicks, MaxInterval.Ticks));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop path.
        }
    }
}
