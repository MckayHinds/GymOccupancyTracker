using System.Collections.Concurrent;

public class OccupancyStore
{
    private readonly TimeSpan _presenceWindow = TimeSpan.FromHours(1);
    private readonly TimeSpan _debounceWindow = TimeSpan.FromSeconds(3);

    private readonly ConcurrentQueue<DateTime> _events = new();
    private DateTime _lastAcceptedUtc = DateTime.MinValue;
    private readonly object _debounceLock = new();

    // store latest message for UI display
    private volatile string _latestMessage = "";
    private volatile DateTime _latestMessageUtc = DateTime.MinValue;

    private void PruneOld()
    {
        var cutoff = DateTime.UtcNow - _presenceWindow;
        while (_events.TryPeek(out var ts) && ts < cutoff)
            _events.TryDequeue(out _);
    }

    private bool TryAcceptTrigger(DateTime nowUtc)
    {
        lock (_debounceLock)
        {
            if (nowUtc - _lastAcceptedUtc < _debounceWindow)
                return false;

            _lastAcceptedUtc = nowUtc;
            return true;
        }
    }

    public bool RecordEntry(string source, string rawMessage, out int activeLastHour)
    {
        var nowUtc = DateTime.UtcNow;

        _latestMessage = $"{source}: {rawMessage}";
        _latestMessageUtc = nowUtc;

        // debounce so one physical event doesn't count multiple times
        if (!TryAcceptTrigger(nowUtc))
        {
            activeLastHour = ActiveCount();
            return false;
        }

        _events.Enqueue(nowUtc);
        activeLastHour = ActiveCount();
        return true;
    }

    public int ActiveCount()
    {
        PruneOld();
        return _events.Count;
    }

    public (string message, DateTime utc) Latest()
        => (_latestMessage, _latestMessageUtc);

    public (TimeSpan presenceWindow, TimeSpan debounceWindow) Settings()
        => (_presenceWindow, _debounceWindow);
}
