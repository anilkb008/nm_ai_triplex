using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using TripletexMcpServer.Client;

namespace TripletexMcpServer.Tools;

[McpServerToolType]
public static class LedgerTools
{
    [McpServerTool(Name = "get_accounts"), Description("Get the chart of accounts (ledger accounts).")]
    public static async Task<string> GetAccounts(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Max results")] int count = 100)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/ledger/account",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["fields"] = "id,number,name,type"
            });
    }

    [McpServerTool(Name = "get_vat_types"), Description("Get all VAT types.")]
    public static async Task<string> GetVatTypes(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/vat");
    }

    [McpServerTool(Name = "search_vouchers"), Description("Search for accounting vouchers.")]
    public static async Task<string> SearchVouchers(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Start date YYYY-MM-DD")] string? dateFrom = null,
        [Description("End date YYYY-MM-DD")] string? dateTo = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/ledger/voucher",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["dateFrom"] = dateFrom ?? "", ["dateTo"] = dateTo ?? ""
            });
    }

    [McpServerTool(Name = "create_voucher"), Description("Create a manual accounting voucher (bilag). Postings must balance (debits = credits).")]
    public static async Task<string> CreateVoucher(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Voucher date YYYY-MM-DD")] string date,
        [Description("Voucher description")] string? description = null,
        [Description("Postings as JSON: [{\"account\":{\"id\":1900},\"amountGrossCurrency\":1000,\"description\":\"...\"}]")]
        string? postingsJson = null)
    {
        var body = new Dictionary<string, object?> { ["date"] = date };
        if (description != null) body["description"] = description;
        if (postingsJson != null)
        {
            try { body["postings"] = JsonSerializer.Deserialize<object>(postingsJson); }
            catch { /* ignore */ }
        }

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/ledger/voucher", body);
    }

    [McpServerTool(Name = "get_currencies"), Description("Get all currencies.")]
    public static async Task<string> GetCurrencies(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/currency",
            new Dictionary<string, string> { ["count"] = "200" });
    }

    [McpServerTool(Name = "get_company_info"), Description("Get company information.")]
    public static async Task<string> GetCompanyInfo(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/company");
    }

    [McpServerTool(Name = "search_products"), Description("Search for products.")]
    public static async Task<string> SearchProducts(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Filter by name")] string? name = null,
        [Description("Filter by number")] string? number = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/product",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["name"] = name ?? "", ["number"] = number ?? ""
            });
    }

    [McpServerTool(Name = "create_product"), Description("Create a new product or service.")]
    public static async Task<string> CreateProduct(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Product name")] string name,
        [Description("Product number/SKU")] string? number = null,
        [Description("Sales price excluding VAT")] double? priceExcludingVat = null,
        [Description("VAT type ID")] int? vatTypeId = null)
    {
        var body = new Dictionary<string, object?> { ["name"] = name };
        if (number != null) body["number"] = number;
        if (priceExcludingVat.HasValue) body["priceExcludingVatCurrency"] = priceExcludingVat.Value;
        if (vatTypeId.HasValue) body["vatType"] = new { id = vatTypeId.Value };

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/product", body);
    }

    [McpServerTool(Name = "tripletex_generic_get"), Description("Generic GET to any Tripletex endpoint.")]
    public static async Task<string> GenericGet(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Endpoint path e.g. /employee or /project/1234")] string endpoint,
        [Description("Query params as key=value pairs separated by &")] string? queryString = null)
    {
        var query = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(queryString))
        {
            foreach (var part in queryString.Split('&'))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2) query[kv[0]] = kv[1];
            }
        }
        return await TripletexClient.GetAsync(baseUrl, sessionToken, endpoint, query);
    }

    [McpServerTool(Name = "tripletex_generic_post"), Description("Generic POST to any Tripletex endpoint.")]
    public static async Task<string> GenericPost(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Endpoint path")] string endpoint,
        [Description("Request body as JSON string")] string bodyJson)
    {
        var body = JsonSerializer.Deserialize<object>(bodyJson) ?? new object();
        return await TripletexClient.PostAsync(baseUrl, sessionToken, endpoint, body);
    }
}
