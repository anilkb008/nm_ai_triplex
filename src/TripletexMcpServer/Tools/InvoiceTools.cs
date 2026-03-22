using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using TripletexMcpServer.Client;

namespace TripletexMcpServer.Tools;

[McpServerToolType]
public static class InvoiceTools
{
    [McpServerTool(Name = "search_invoices"), Description("Search for invoices.")]
    public static async Task<string> SearchInvoices(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Start date filter YYYY-MM-DD")] string? invoiceDateFrom = null,
        [Description("End date filter YYYY-MM-DD")] string? invoiceDateTo = null,
        [Description("Filter by customer ID")] string? customerId = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/invoice",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["invoiceDateFrom"] = invoiceDateFrom ?? "",
                ["invoiceDateTo"] = invoiceDateTo ?? "",
                ["customerId"] = customerId ?? ""
            });
    }

    [McpServerTool(Name = "get_invoice"), Description("Get a specific invoice by ID.")]
    public static async Task<string> GetInvoice(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Invoice ID")] int id)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, $"/invoice/{id}");
    }

    [McpServerTool(Name = "create_invoice"), Description("Create a new customer invoice. Provide invoice lines as JSON.")]
    public static async Task<string> CreateInvoice(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Customer ID")] int customerId,
        [Description("Invoice date YYYY-MM-DD")] string invoiceDate,
        [Description("Due date YYYY-MM-DD")] string? dueDate = null,
        [Description("Invoice lines as JSON array: [{\"description\":\"...\",\"quantity\":1,\"unitPriceExcludingVatCurrency\":100}]")]
        string? invoiceLinesJson = null,
        [Description("Comment / memo")] string? comment = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["customer"] = new { id = customerId },
            ["invoiceDate"] = invoiceDate
        };
        if (dueDate != null) body["dueDate"] = dueDate;
        if (comment != null) body["comment"] = comment;
        if (invoiceLinesJson != null)
        {
            try { body["invoiceLines"] = JsonSerializer.Deserialize<object>(invoiceLinesJson); }
            catch { /* ignore parse errors */ }
        }

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/invoice", body);
    }

    [McpServerTool(Name = "send_invoice"), Description("Send an invoice to the customer.")]
    public static async Task<string> SendInvoice(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Invoice ID")] int id,
        [Description("Send type: EMAIL, EHF, AVTALEGIRO, VIPPS")] string sendType = "EMAIL")
    {
        return await TripletexClient.PutAsync(baseUrl, sessionToken, $"/invoice/{id}/:send", new { },
            new Dictionary<string, string> { ["sendType"] = sendType });
    }
}
