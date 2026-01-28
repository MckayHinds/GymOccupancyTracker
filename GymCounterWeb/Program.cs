using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// If API_KEY is empty or missing, no key is required.
var apiKey = builder.Configuration["API_KEY"] ?? "";


builder.WebHost.UseUrls("http://0.0.0.0:5168");

var app = builder.Build();

// Serve static site from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// -------------------- Configuration --------------------
TimeSpan presenceWindow = TimeSpan.FromHours(1);     // how long someone stays “counted”
TimeSpan debounceWindow = TimeSpan.FromSeconds(3);   // ignore repeat triggers within 3 seconds

// -------------------- In-memory event store --------------------
var events = new ConcurrentQueue<DateTime>();

DateTime lastAcceptedUtc = DateTime.MinValue;
object debounceLock = new();

// Remove events older than the presence window so Count stays accurate and memory doesn't grow forever.
void PruneOld()
{
    var cutoff = DateTime.UtcNow - presenceWindow;

    while (events.TryPeek(out var ts) && ts < cutoff)
        events.TryDequeue(out _);
}

// Debounce: accept at most one event within debounceWindow.
bool TryAcceptTrigger(out DateTime acceptedUtc)
{
    acceptedUtc = DateTime.UtcNow;

    lock (debounceLock)
    {
        if (acceptedUtc - lastAcceptedUtc < debounceWindow)
            return false;

        lastAcceptedUtc = acceptedUtc;
        return true;
    }
}

int ActiveCount()
{
    PruneOld();
    return events.Count;
}

// -------------------- API Endpoints --------------------

// { "source": "front-door", "apiKey": "YOUR_API_KEY" }
app.MapPost("/api/entry", (EntryRequest req) =>
{
    // API key check (optional)
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        if (string.IsNullOrWhiteSpace(req.ApiKey) || !string.Equals(req.ApiKey, apiKey, StringComparison.Ordinal))
            return Results.Unauthorized();
    }

    // Debounce (so one door-open doesn't count multiple times)
    if (!TryAcceptTrigger(out var nowUtc))
    {
        return Results.Ok(new
        {
            accepted = false,
            reason = "debounced",
            activeLastHour = ActiveCount()
        });
    }

    events.Enqueue(nowUtc);

    return Results.Ok(new
    {
        accepted = true,
        recordedAtUtc = nowUtc,
        source = req.Source ?? "unknown",
        activeLastHour = ActiveCount()
    });
});

// GET /api/occupancy
app.MapGet("/api/occupancy", () =>
{
    var cutoff = DateTime.UtcNow - presenceWindow;

    return Results.Ok(new
    {
        activeLastHour = ActiveCount(),
        cutoffUtc = cutoff,
        windowMinutes = presenceWindow.TotalMinutes
    });
});

// GET /api/health (quick test endpoint)
app.MapGet("/api/health", () => Results.Ok(new { ok = true, timeUtc = DateTime.UtcNow }));

app.Run();

record EntryRequest(string? Source, string? ApiKey);
