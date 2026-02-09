using System.Collections.Concurrent;

public class OccupancyStore
{
    private readonly TimeSpan _presenceWindow = TimeSpan.FromHours(1);     // how long someone stays “counted”
    private readonly TimeSpan _debounceWindow = TimeSpan.FromSeconds(3);   // ignore repeat triggers within 3 seconds

    private readonly ConcurrentQueue<DateTime> _events = new();

    private DateTime _lastAcceptedUtc = DateTime.MinValue;
    private readonly object _debounceLock = new();

    // Latest message storage (thread-safe)
    private readonly object _latestLock = new();
    private string _latestMessage = "";
    private DateTime _latestMessageUtc = DateTime.MinValue;

    // Remove events older than the presence window so Count stays accurate and memory doesn't grow forever.
    private void PruneOld()
    {
        var cutoff = DateTime.UtcNow - _presenceWindow;

        while (_events.TryPeek(out var ts) && ts < cutoff)
            _events.TryDequeue(out _);
    }

    // Debounce: accept at most one event within debounceWindow.
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

        // Store latest message
        lock (_latestLock)
        {
            _latestMessage = $"{source}: {rawMessage}";
            _latestMessageUtc = nowUtc;
        }

        // Debounce
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
    {
        lock (_latestLock)
        {
            return (_latestMessage, _latestMessageUtc);
        }
    }

    public (TimeSpan presenceWindow, TimeSpan debounceWindow) Settings()
        => (_presenceWindow, _debounceWindow);
}
