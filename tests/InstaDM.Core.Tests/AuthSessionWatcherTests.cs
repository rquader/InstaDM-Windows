using InstaDM.Core.Authentication;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class AuthSessionWatcherTests
{
    /// <summary>Probe fake: scripted answers, then holds the last one.</summary>
    private sealed class FakeProbe(params bool[] answers) : ISessionCookieProbe
    {
        private int _index;
        public int Calls => _index;

        public Task<bool> SessionCookieExistsAsync(CancellationToken cancellationToken)
        {
            var i = Math.Min(_index, answers.Length - 1);
            _index++;
            return Task.FromResult(answers[i]);
        }
    }

    private sealed class ThrowingProbe : ISessionCookieProbe
    {
        public int Calls;
        public Task<bool> SessionCookieExistsAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Calls == 1
                ? throw new InvalidOperationException("probe fault")
                : Task.FromResult(true);
        }
    }

    /// <summary>Deterministic delay: records requested intervals, completes
    /// immediately, and lets the test stop the loop after N sleeps.</summary>
    private sealed class ManualDelay
    {
        public readonly List<TimeSpan> Requested = [];
        public int StopAfter { get; init; } = int.MaxValue;
        public Action? OnStop { get; set; }

        public async Task Delay(TimeSpan interval, CancellationToken token)
        {
            lock (Requested) { Requested.Add(interval); }
            if (Requested.Count >= StopAfter)
            {
                OnStop?.Invoke();
                token.ThrowIfCancellationRequested();
            }
            // Yield so the loop cannot spin a core between fake sleeps.
            await Task.Delay(1, token);
        }
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(2);
        }
        Assert.IsTrue(condition(), "condition not reached in time");
    }

    [TestMethod]
    public async Task DelayedCookieAppearance_IsObserved()
    {
        var probe = new FakeProbe(false, false, true);
        var delay = new ManualDelay();
        using var watcher = new AuthSessionWatcher(probe, delay.Delay);

        var observations = new List<bool>();
        Assert.IsTrue(watcher.Start(observations.Add));

        await WaitUntil(() => observations.Contains(true));
        watcher.Stop();

        CollectionAssert.AreEqual(new[] { false, false, true }, observations[..3]);
    }

    [TestMethod]
    public void DuplicateStart_IsRejected()
    {
        var delay = new ManualDelay();
        using var watcher = new AuthSessionWatcher(new FakeProbe(false), delay.Delay);
        Assert.IsTrue(watcher.Start(_ => { }));
        Assert.IsFalse(watcher.Start(_ => { }), "second Start must not spawn a second loop");
        Assert.IsTrue(watcher.IsRunning);
        watcher.Stop();
        Assert.IsFalse(watcher.IsRunning);
    }

    [TestMethod]
    public async Task Stop_PreventsFurtherObservations()
    {
        var probe = new FakeProbe(false);
        using var watcher = new AuthSessionWatcher(probe, (interval, token) => Task.Delay(1, token));

        var count = 0;
        watcher.Start(_ => Interlocked.Increment(ref count));
        await WaitUntil(() => Volatile.Read(ref count) >= 2);
        watcher.Stop();

        var snapshot = Volatile.Read(ref count);
        await Task.Delay(50);
        // At most one probe already in flight at Stop; nothing beyond that.
        Assert.IsTrue(Volatile.Read(ref count) <= snapshot + 1,
            "observations continued after Stop");
        Assert.IsFalse(watcher.IsRunning);
    }

    [TestMethod]
    public async Task Restart_AfterStop_IsAllowed()
    {
        var delay = new ManualDelay();
        using var watcher = new AuthSessionWatcher(new FakeProbe(true), delay.Delay);
        var first = 0;
        watcher.Start(_ => Interlocked.Increment(ref first));
        await WaitUntil(() => Volatile.Read(ref first) >= 1);
        watcher.Stop();

        var second = 0;
        Assert.IsTrue(watcher.Start(_ => Interlocked.Increment(ref second)));
        await WaitUntil(() => Volatile.Read(ref second) >= 1);
        watcher.Stop();
    }

    [TestMethod]
    public async Task Backoff_GrowsToCapAndStaysThere()
    {
        var delay = new ManualDelay { StopAfter = 12 };
        using var watcher = new AuthSessionWatcher(new FakeProbe(false), delay.Delay);
        delay.OnStop = watcher.Stop;

        watcher.Start(_ => { });
        await WaitUntil(() => delay.Requested.Count >= 12);

        Assert.AreEqual(AuthSessionWatcher.InitialInterval, delay.Requested[0]);
        for (var i = 1; i < delay.Requested.Count; i++)
        {
            Assert.IsTrue(delay.Requested[i] >= delay.Requested[i - 1], $"interval shrank at {i}");
            Assert.IsTrue(delay.Requested[i] <= AuthSessionWatcher.MaxInterval, $"cap exceeded at {i}");
        }
        Assert.AreEqual(AuthSessionWatcher.MaxInterval, delay.Requested[^1], "must reach the cap");
    }

    [TestMethod]
    public async Task ProbeFault_FailsClosedAndKeepsPolling()
    {
        var probe = new ThrowingProbe();
        var delay = new ManualDelay();
        using var watcher = new AuthSessionWatcher(probe, delay.Delay);

        var observations = new List<bool>();
        watcher.Start(observations.Add);
        await WaitUntil(() => observations.Count >= 2);
        watcher.Stop();

        Assert.IsFalse(observations[0], "fault must be reported as absent (fail closed)");
        Assert.IsTrue(observations[1], "loop must survive the fault");
    }

    [TestMethod]
    public void Dispose_StopsTheLoop()
    {
        var watcher = new AuthSessionWatcher(new FakeProbe(false), new ManualDelay().Delay);
        watcher.Start(_ => { });
        watcher.Dispose();
        Assert.IsFalse(watcher.IsRunning);
    }
}
