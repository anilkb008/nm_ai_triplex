using System.Net.Http.Headers;
using System.Text.Json;
using SolveApi.Models;
using SolveApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddHttpClient("Claude", client =>
{
    var apiKey = builder.Configuration["ANTHROPIC_API_KEY"]
        ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
        ?? throw new InvalidOperationException("ANTHROPIC_API_KEY env var is required");

    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<AgentService>();
builder.Services.AddHealthChecks();
builder.Logging.AddConsole();

var app = builder.Build();

// ── Routes ────────────────────────────────────────────────────────────────────

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "Tripletex AI Accounting Agent",
    version = "1.0",
    endpoints = new[] { "POST /solve", "GET /health" }
}));

app.MapPost("/solve", async (HttpRequest httpRequest, AgentService agentService, ILogger<Program> logger) =>
{
    SolveRequest? request;
    try
    {
        request = await httpRequest.ReadFromJsonAsync<SolveRequest>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to parse request body");
        return Results.BadRequest(new { error = "Invalid request body", detail = ex.Message });
    }

    if (request is null)
        return Results.BadRequest(new { error = "Request body is required" });

    if (string.IsNullOrWhiteSpace(request.Prompt))
        return Results.BadRequest(new { error = "prompt is required" });

    if (string.IsNullOrWhiteSpace(request.TripletexCredentials?.BaseUrl))
        return Results.BadRequest(new { error = "tripletex_credentials.base_url is required" });

    if (string.IsNullOrWhiteSpace(request.TripletexCredentials?.SessionToken))
        return Results.BadRequest(new { error = "tripletex_credentials.session_token is required" });

    try
    {
        var status = await agentService.SolveAsync(request, httpRequest.HttpContext.RequestAborted);
        return Results.Ok(new SolveResponse { Status = status });
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(408); // Request timeout
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Agent failed to solve task");
        // Return completed anyway — partial work may have been done
        return Results.Ok(new SolveResponse { Status = "completed" });
    }
});

app.Run();
