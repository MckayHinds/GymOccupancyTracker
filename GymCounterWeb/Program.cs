using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// If API_KEY is empty, no key is required.
var apiKey = builder.Configuration["API_KEY"] ?? "";

var app = builder.Build();

// Serve static site from wwwroot (index.html, styles.css, app.js)
app.UseDefaultFiles();
app.UseStaticFiles();

// -------------------- Configuration --------------------
TimeSpan presenceWindow = TimeSpan.FromHours(1);     // how long a person stays “counted”
TimeSpan debounceWindow = TimeSpan.FromSeconds(3);   // ignore repeat triggers within 3s

// -------------------- In-memory event store --------------------
var events = new ConcurrentQueue<DateTime>();

DateTime lastAcceptedUtc = DateTime.MinValue;
object debounceLock = new();

void PruneOld()
{
    var cutoff = DateTime.UtcNow - presenceWindow;
    while (events.TryPeek(out var ts) && ts < cutoff)
        events.TryDequeue(out _);
}

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
    // Prune then Count
    PruneOld();
    return events.Count;
}

// -------------------- API Endpoints --------------------

app.MapPost("/api/entry", (EntryRequest req) =>
{
    // If an API key is configured, require it
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        if (string.IsNullOrWhiteSpace(req.ApiKey) || req.ApiKey != apiKey)
            return Results.Unauthorized();
    }

    // Debounce so one door-open doesn’t count 5 times
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

// Helpful ping
app.MapGet("/api/health", () => Results.Ok(new { ok = true, timeUtc = DateTime.UtcNow }));

app.Run();

record EntryRequest(string? Source, string? ApiKey);
