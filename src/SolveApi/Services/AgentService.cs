using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SolveApi.Models;
using SolveApi.Tools;

namespace SolveApi.Services;

public class AgentService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AgentService> _logger;
    private readonly string _anthropicApiKey;
    private const string ClaudeModel = "claude-sonnet-4-6";
    private const int MaxIterations = 25;
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
        _anthropicApiKey = config["ANTHROPIC_API_KEY"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured");
    }

    public async Task<string> SolveAsync(SolveRequest request, CancellationToken ct = default)
    {
        using var tripletex = new TripletexApiClient(
            request.TripletexCredentials.BaseUrl,
            request.TripletexCredentials.SessionToken);

        var systemPrompt = BuildSystemPrompt();
        var messages = new List<object> { BuildUserMessage(request) };

        _logger.LogInformation("Starting agent loop for task: {Prompt}", request.Prompt[..Math.Min(100, request.Prompt.Length)]);

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            var claudeResponse = await CallClaudeAsync(systemPrompt, messages, ct);

            if (claudeResponse.Error != null)
            {
                _logger.LogError("Claude API error: {Error}", claudeResponse.Error.Message);
                throw new Exception($"Claude API error: {claudeResponse.Error.Message}");
            }

            // Serialize the content list and add as assistant message
            var assistantContent = claudeResponse.Content
                .Select(el => (object)el)
                .ToList();

            messages.Add(new { role = "assistant", content = assistantContent });

            _logger.LogInformation("Iteration {I}: stop_reason={StopReason}", iteration + 1, claudeResponse.StopReason);

            if (claudeResponse.StopReason == "end_turn" || claudeResponse.StopReason == "max_tokens")
                break;

            if (claudeResponse.StopReason == "tool_use")
            {
                var toolResults = new List<object>();

                foreach (var block in claudeResponse.Content)
                {
                    if (block.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "tool_use")
                    {
                        var toolId = block.GetProperty("id").GetString()!;
                        var toolName = block.GetProperty("name").GetString()!;
                        var toolInput = block.GetProperty("input");

                        _logger.LogInformation("Executing tool: {Tool}", toolName);

                        var (resultJson, isError) = await ExecuteToolAsync(toolName, toolInput, tripletex, ct);

                        toolResults.Add(new
                        {
                            type = "tool_result",
                            tool_use_id = toolId,
                            content = resultJson,
                            is_error = isError ? (bool?)true : null
                        });
                    }
                }

                messages.Add(new { role = "user", content = toolResults });
            }
        }

        return "completed";
    }

    // ── Claude API call ───────────────────────────────────────────────────────

    private async Task<ClaudeResponse> CallClaudeAsync(string system, List<object> messages, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("Claude");

        var requestBody = new
        {
            model = ClaudeModel,
            max_tokens = MaxTokens,
            system,
            tools = ToolDefinitions.All.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = t.InputSchema
            }).ToList(),
            messages
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await http.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Claude API HTTP {Status}: {Body}", (int)response.StatusCode, body);
        }

        return JsonSerializer.Deserialize<ClaudeResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ClaudeResponse { Error = new ClaudeError { Message = "Failed to deserialize response" } };
    }

    // ── Tool executor ─────────────────────────────────────────────────────────

    private async Task<(string result, bool isError)> ExecuteToolAsync(
        string toolName, JsonElement input, TripletexApiClient tripletex, CancellationToken ct)
    {
        try
        {
            var result = toolName switch
            {
                // Company
                "get_company_info" => await tripletex.GetCompanyInfoAsync(),
                "get_accounts" => await tripletex.GetAccountsAsync(),
                "get_currencies" => await tripletex.GetCurrenciesAsync(),
                "get_vat_types" => await tripletex.GetVatTypesAsync(),
                "get_departments" => await tripletex.GetDepartmentsAsync(),
                "create_department" => await tripletex.CreateDepartmentAsync(input.Deserialize<object>()!),

                // Employees
                "search_employees" => await tripletex.SearchEmployeesAsync(
                    firstName: GetString(input, "firstName"),
                    lastName: GetString(input, "lastName"),
                    email: GetString(input, "email"),
                    count: GetInt(input, "count", 50)),
                "get_employee" => await tripletex.GetEmployeeAsync(GetInt(input, "id")),
                "create_employee" => await tripletex.CreateEmployeeAsync(input.Deserialize<object>()!),
                "update_employee" => await tripletex.UpdateEmployeeAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Customers
                "search_customers" => await tripletex.SearchCustomersAsync(
                    name: GetString(input, "name"),
                    email: GetString(input, "email"),
                    organizationNumber: GetString(input, "organizationNumber"),
                    count: GetInt(input, "count", 50)),
                "get_customer" => await tripletex.GetCustomerAsync(GetInt(input, "id")),
                "create_customer" => await tripletex.CreateCustomerAsync(input.Deserialize<object>()!),
                "update_customer" => await tripletex.UpdateCustomerAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Suppliers
                "search_suppliers" => await tripletex.SearchSuppliersAsync(
                    name: GetString(input, "name"),
                    organizationNumber: GetString(input, "organizationNumber"),
                    count: GetInt(input, "count", 50)),
                "get_supplier" => await tripletex.GetSupplierAsync(GetInt(input, "id")),
                "create_supplier" => await tripletex.CreateSupplierAsync(input.Deserialize<object>()!),
                "update_supplier" => await tripletex.UpdateSupplierAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Products
                "search_products" => await tripletex.SearchProductsAsync(
                    name: GetString(input, "name"),
                    number: GetString(input, "number"),
                    count: GetInt(input, "count", 50)),
                "get_product" => await tripletex.GetProductAsync(GetInt(input, "id")),
                "create_product" => await tripletex.CreateProductAsync(input.Deserialize<object>()!),
                "update_product" => await tripletex.UpdateProductAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Invoices
                "search_invoices" => await tripletex.SearchInvoicesAsync(
                    invoiceDateFrom: GetString(input, "invoiceDateFrom"),
                    invoiceDateTo: GetString(input, "invoiceDateTo"),
                    customerId: GetString(input, "customerId"),
                    count: GetInt(input, "count", 50)),
                "get_invoice" => await tripletex.GetInvoiceAsync(GetInt(input, "id")),
                "create_invoice" => await tripletex.CreateInvoiceAsync(input.Deserialize<object>()!),
                "send_invoice" => await tripletex.SendInvoiceAsync(
                    GetInt(input, "id"),
                    GetString(input, "sendType") ?? "EMAIL"),

                // Orders
                "search_orders" => await tripletex.SearchOrdersAsync(
                    customerId: GetString(input, "customerId"),
                    count: GetInt(input, "count", 50)),
                "create_order" => await tripletex.CreateOrderAsync(input.Deserialize<object>()!),

                // Projects
                "search_projects" => await tripletex.SearchProjectsAsync(
                    name: GetString(input, "name"),
                    number: GetString(input, "number"),
                    count: GetInt(input, "count", 50)),
                "get_project" => await tripletex.GetProjectAsync(GetInt(input, "id")),
                "create_project" => await tripletex.CreateProjectAsync(input.Deserialize<object>()!),
                "update_project" => await tripletex.UpdateProjectAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Timesheet
                "search_timesheet_entries" => await tripletex.SearchTimesheetEntriesAsync(
                    dateFrom: GetString(input, "dateFrom"),
                    dateTo: GetString(input, "dateTo"),
                    employeeId: GetString(input, "employeeId"),
                    projectId: GetString(input, "projectId"),
                    count: GetInt(input, "count", 50)),
                "create_timesheet_entry" => await tripletex.CreateTimesheetEntryAsync(input.Deserialize<object>()!),
                "update_timesheet_entry" => await tripletex.UpdateTimesheetEntryAsync(
                    GetInt(input, "id"), input.Deserialize<object>()!),

                // Vouchers
                "search_vouchers" => await tripletex.SearchVouchersAsync(
                    dateFrom: GetString(input, "dateFrom"),
                    dateTo: GetString(input, "dateTo"),
                    count: GetInt(input, "count", 50)),
                "get_voucher" => await tripletex.GetVoucherAsync(GetInt(input, "id")),
                "create_voucher" => await tripletex.CreateVoucherAsync(input.Deserialize<object>()!),

                // Postings
                "search_postings" => await tripletex.SearchPostingsAsync(
                    dateFrom: GetString(input, "dateFrom"),
                    dateTo: GetString(input, "dateTo"),
                    count: GetInt(input, "count", 100)),

                // Generic fallback
                "tripletex_get" => await tripletex.GenericGetAsync(
                    GetString(input, "endpoint")!,
                    GetDictionary(input, "params")),
                "tripletex_post" => await tripletex.GenericPostAsync(
                    GetString(input, "endpoint")!,
                    GetElement(input, "body")?.Deserialize<object>() ?? new object()),

                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
            };

            _logger.LogDebug("Tool {Tool} result: {Result}", toolName, result[..Math.Min(200, result.Length)]);
            return (result, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {Tool} threw exception", toolName);
            return (JsonSerializer.Serialize(new { error = ex.Message }), true);
        }
    }

    // ── Message builders ──────────────────────────────────────────────────────

    private static object BuildUserMessage(SolveRequest request)
    {
        var contentBlocks = new List<object>();

        // Attach files
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

        return new { role = "user", content = contentBlocks };
    }

    private static string BuildSystemPrompt() => """
        You are an expert AI accounting agent for Tripletex, a Norwegian accounting and ERP system.

        Your task is to complete accounting and business administration tasks using the Tripletex API.
        Tasks may be provided in any of these languages: Norwegian (Bokmål/Nynorsk), English, Swedish, Danish, German, French, Spanish.
        Understand the task regardless of language, but always interact with the Tripletex API in the correct format.

        ## Guidelines

        1. **Read the task carefully** — understand exactly what needs to be created, updated, or retrieved.
        2. **Use minimum API calls** — be efficient; don't search unnecessarily when you can create directly.
        3. **Use the right tools** — prefer specific tools (e.g. create_employee) over generic fallbacks.
        4. **Dates** — always use YYYY-MM-DD format. Today's date context: use the current year for any relative dates.
        5. **Norwegian specifics** — organization numbers (org.nr) are 9 digits; VAT (mva) is typically 25%; NOK is the default currency.
        6. **IDs for references** — when creating records that reference other entities (customer, employee, product), first search for or create those entities and use their IDs.
        7. **Vouchers** — debit and credit postings must balance (sum to zero).
        8. **Files** — if a file is attached (PDF, image), read it carefully to extract data like invoice amounts, dates, customer details.

        ## Common Task Patterns

        - "Create employee" → create_employee with required fields
        - "Create customer/client" → create_customer
        - "Create invoice" → possibly create_customer first, then create_invoice with lines
        - "Register hours" → find project + employee, then create_timesheet_entry
        - "Create voucher/bilag" → get_accounts for correct account numbers, then create_voucher
        - "Create project" → create_project with dates and customer

        When the task is complete, simply stop — do not make unnecessary extra calls.
        """;

    // ── JSON helpers ──────────────────────────────────────────────────────────

    private static string? GetString(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static int GetInt(JsonElement el, string key, int defaultValue = 0)
    {
        if (el.TryGetProperty(key, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetInt32();
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var v)) return v;
        }
        return defaultValue;
    }

    private static JsonElement? GetElement(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop)) return prop;
        return null;
    }

    private static Dictionary<string, string>? GetDictionary(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Object)
            return null;

        var dict = new Dictionary<string, string>();
        foreach (var kv in prop.EnumerateObject())
        {
            if (kv.Value.ValueKind == JsonValueKind.String)
                dict[kv.Name] = kv.Value.GetString()!;
            else
                dict[kv.Name] = kv.Value.ToString();
        }
        return dict;
    }
}
