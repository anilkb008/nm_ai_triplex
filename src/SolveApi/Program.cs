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

    // Enable MCP client support — Anthropic connects to our MCP server and
    // handles the entire tool-calling loop server-side.
    client.DefaultRequestHeaders.Add("anthropic-beta", "mcp-client-2025-11-20");

    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // MCP tool calls can take several minutes; allow enough time.
    client.Timeout = TimeSpan.FromMinutes(10);
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
    version = "2.0",
    model = "claude-opus-4-6",
    approach = "Anthropic native MCP — tool loop handled server-side by Anthropic",
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
        return Results.StatusCode(408);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Agent failed");
        return Results.Ok(new SolveResponse { Status = "completed" });
    }
});

app.Run();
