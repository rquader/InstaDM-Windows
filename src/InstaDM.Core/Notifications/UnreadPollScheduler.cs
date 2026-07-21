namespace InstaDM.Core.Notifications;

/// <summary>
/// Bounded title-poll loop. The host supplies a title reader that returns
/// <c>document.title</c> (or the WebView2 DocumentTitle) without reading
/// message DOM. Cancelled on dispose / app shutdown.
/// </summary>
public sealed class UnreadPollScheduler : IDisposable
{
    private readonly Func<CancellationToken, Task<string?>> _readTitle;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private TimeSpan _interval;

    public UnreadPollScheduler(
        Func<CancellationToken, Task<string?>> readTitle,
        TimeSpan interval,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _readTitle = readTitle;
        _interval = interval;
        _delay = delay ?? Task.Delay;
    }

    public bool IsRunning
    {
        get { lock (_gate) { return _cts is not null; } }
    }

    public void SetInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        _interval = interval;
    }

    public bool Start(Action<string?> onTitle)
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

        _ = RunAsync(onTitle, cts.Token);
        return true;
    }

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

    private async Task RunAsync(Action<string?> onTitle, CancellationToken token)
    {
        await Task.Yield();
        try
        {
            while (!token.IsCancellationRequested)
            {
                string? title = null;
                try
                {
                    title = await _readTitle(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    title = null;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                onTitle(title);
                await _delay(_interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // normal stop
        }
    }
}
