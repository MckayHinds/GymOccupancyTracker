using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

// If API_KEY is empty or missing, no key is required.
var apiKey = builder.Configuration["API_KEY"] ?? "";

builder.WebHost.UseUrls("http://0.0.0.0:5168");

// Store + MQTT background service
builder.Services.AddSingleton<OccupancyStore>();
builder.Services.AddHostedService<MqttListenerService>();

var app = builder.Build();

app.UseCors();

// Serve static site from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// -------------------- API Endpoints --------------------

// { "source": "front-door", "apiKey": " " }
app.MapPost("/api/entry", (EntryRequest req, OccupancyStore store) =>
{
    // API key check
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        if (string.IsNullOrWhiteSpace(req.ApiKey) || !string.Equals(req.ApiKey, apiKey, StringComparison.Ordinal))
            return Results.Unauthorized();
    }

    var accepted = store.RecordEntry(
        source: req.Source ?? "http:unknown",
        rawMessage: "http-trigger",
        out var active
    );

    if (!accepted)
    {
        return Results.Ok(new
        {
            accepted = false,
            reason = "debounced",
            activeLastHour = active
        });
    }

    return Results.Ok(new
    {
        accepted = true,
        recordedAtUtc = DateTime.UtcNow,
        source = req.Source ?? "http:unknown",
        activeLastHour = active
    });
});

// GET /api/occupancy
app.MapGet("/api/occupancy", (OccupancyStore store) =>
{
    var (presenceWindow, _) = store.Settings();
    var cutoff = DateTime.UtcNow - presenceWindow;

    return Results.Ok(new
    {
        activeLastHour = store.ActiveCount(),
        cutoffUtc = cutoff,
        windowMinutes = presenceWindow.TotalMinutes
    });
});

app.MapGet("/api/latest", (OccupancyStore store) =>
{
    var (msg, utc) = store.Latest();
    return Results.Ok(new
    {
        latestMessage = msg,
        latestMessageUtc = utc
    });
});

// GET /api/health (quick test endpoint)
app.MapGet("/api/health", () => Results.Ok(new { ok = true, timeUtc = DateTime.UtcNow }));

app.Run();

record EntryRequest(string? Source, string? ApiKey);
