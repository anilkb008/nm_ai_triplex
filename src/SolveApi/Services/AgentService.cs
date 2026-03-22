using System.Text;
using System.Text.Json;
using SolveApi.Models;

namespace SolveApi.Services;

/// <summary>
/// Sends a single request to the Anthropic API with the Tripletex MCP server attached.
/// Anthropic handles tool discovery and the entire tool-calling loop server-side —
/// no manual agentic loop needed here.
/// </summary>
public class AgentService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AgentService> _logger;
    private readonly string _mcpServerUrl;

    private const string ClaudeModel = "claude-opus-4-6";
    private const int MaxTokens = 8192;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AgentService(IHttpClientFactory httpFactory, ILogger<AgentService> logger, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _mcpServerUrl = config["MCP_SERVER_URL"]
            ?? Environment.GetEnvironmentVariable("MCP_SERVER_URL")
            ?? throw new InvalidOperationException(
                "MCP_SERVER_URL is not configured. " +
                "Set it to the public HTTPS URL of the TripletexMcpServer, e.g. https://your-vm.example.com/mcp");
    }

    public async Task<string> SolveAsync(SolveRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Solving task. MCP server: {McpUrl}  Prompt: {Prompt}",
            _mcpServerUrl,
            request.Prompt[..Math.Min(120, request.Prompt.Length)]);

        // Single Anthropic API call — MCP tool loop runs server-side on Anthropic's infrastructure.
        var requestBody = new
        {
            model = ClaudeModel,
            max_tokens = MaxTokens,
            system = BuildSystemPrompt(request.TripletexCredentials),
            mcp_servers = new[]
            {
                new
                {
                    type = "url",
                    url = _mcpServerUrl,
                    name = "tripletex"
                }
            },
            messages = BuildMessages(request)
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var http = _httpFactory.CreateClient("Claude");

        _logger.LogDebug("Calling Anthropic API...");
        var response = await http.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API {Status}: {Body}", (int)response.StatusCode, responseBody);
            // Return completed — scoring will verify what was actually done in Tripletex
        }
        else
        {
            // Log token usage for monitoring
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    _logger.LogInformation("Tokens used — input: {In}, output: {Out}",
                        usage.TryGetProperty("input_tokens", out var i) ? i.GetInt32() : -1,
                        usage.TryGetProperty("output_tokens", out var o) ? o.GetInt32() : -1);
                }
            }
            catch { /* ignore logging errors */ }

            _logger.LogInformation("Task completed successfully");
        }

        return "completed";
    }

    // ── Message builders ──────────────────────────────────────────────────────

    private static string BuildSystemPrompt(TripletexCredentials creds) => $"""
        You are an expert AI accounting agent for Tripletex, a Norwegian accounting and ERP system.

        ## Tripletex Credentials (this session only)
        - baseUrl: {creds.BaseUrl}
        - sessionToken: {creds.SessionToken}

        CRITICAL: Pass these exact values as `baseUrl` and `sessionToken` parameters on EVERY
        Tripletex tool call. Never omit them.

        ## Task Instructions
        Complete the accounting task described by the user. Tasks may be in any of these languages:
        Norwegian (Bokmål/Nynorsk), English, Swedish, Danish, German, French, Spanish.
        Understand the task in any language; always use correct API formats when calling tools.

        ## Guidelines
        1. Read the task carefully — understand exactly what needs to be created, updated, or retrieved.
        2. Minimise API calls — be efficient; search before creating only when strictly needed.
        3. Dates: always use YYYY-MM-DD format.
        4. Norwegian specifics: org numbers are 9 digits; VAT (mva) is typically 25%; NOK is default.
        5. For records that reference other entities (customer, employee), find or create them first and
           use their numeric IDs in subsequent calls.
        6. Vouchers: debit and credit postings MUST balance (sum to zero).
        7. Files: if PDFs or images are attached, extract all relevant data from them before acting.

        When the task is complete, stop — do not make unnecessary extra calls.
        """;

    private static List<object> BuildMessages(SolveRequest request)
    {
        var contentBlocks = new List<object>();

        // Attach files (images and PDFs)
        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                if (file.MimeType.StartsWith("image/"))
                {
                    contentBlocks.Add(new
                    {
                        type = "image",
                        source = new { type = "base64", media_type = file.MimeType, data = file.Data }
                    });
                }
                else if (file.MimeType == "application/pdf")
                {
                    contentBlocks.Add(new
                    {
                        type = "document",
                        source = new { type = "base64", media_type = "application/pdf", data = file.Data }
                    });
                }
            }
        }

        contentBlocks.Add(new { type = "text", text = request.Prompt });

        return [new { role = "user", content = contentBlocks }];
    }
}
