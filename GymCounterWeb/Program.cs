using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Serve wwwroot/index.html automatically
app.UseDefaultFiles();
app.UseStaticFiles();

// ----- Config -----
TimeSpan presenceWindow = TimeSpan.FromHours(1);
TimeSpan debounceWindow = TimeSpan.FromSeconds(3);

// ----- In-memory store -----
var events = new ConcurrentQueue<DateTime>();
DateTime lastAcceptedUtc = DateTime.MinValue;
object debounceLock = new();

void Prune()
{
    var cutoff = DateTime.UtcNow - presenceWindow;
    while (events.TryPeek(out var ts) && ts < cutoff)
        events.TryDequeue(out _);
}

bool TryAccept(out DateTime acceptedUtc)
{
    acceptedUtc = DateTime.UtcNow;
    lock (debounceLock)
    {
        if (acceptedUtc - lastAcceptedUtc < debounceWindow) return false;
        lastAcceptedUtc = acceptedUtc;
        return true;
    }
}

// ----- API -----

app.MapPost("/api/entry", (EntryRequest req) =>
{
    if (!TryAccept(out var nowUtc))
    {
        Prune();
        return Results.Ok(new { accepted = false, reason = "debounced", activeLastHour = events.Count });
    }

    events.Enqueue(nowUtc);
    Prune();
    return Results.Ok(new { accepted = true, recordedAtUtc = nowUtc, activeLastHour = events.Count });
});

app.MapGet("/api/occupancy", () =>
{
    Prune();
    var cutoff = DateTime.UtcNow - presenceWindow;
    return Results.Ok(new { activeLastHour = events.Count, cutoffUtc = cutoff });
});

app.Run();

record EntryRequest(string? Source);
