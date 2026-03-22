using TripletexMcpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// ── MCP Server with SSE transport ────────────────────────────────────────────

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<EmployeeTools>()
    .WithTools<CustomerTools>()
    .WithTools<InvoiceTools>()
    .WithTools<ProjectTools>()
    .WithTools<LedgerTools>();

builder.Logging.AddConsole();

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    service = "Tripletex MCP Server",
    description = "Model Context Protocol server exposing Tripletex API tools",
    transport = "SSE",
    endpoint = "/mcp",
    tools = new[]
    {
        "get_company_info", "get_accounts", "get_currencies", "get_vat_types", "get_departments",
        "search_employees", "get_employee", "create_employee", "update_employee",
        "search_customers", "get_customer", "create_customer", "update_customer",
        "search_suppliers", "create_supplier",
        "search_products", "create_product",
        "search_invoices", "get_invoice", "create_invoice", "send_invoice",
        "search_projects", "get_project", "create_project",
        "search_timesheet_entries", "create_timesheet_entry",
        "search_vouchers", "create_voucher",
        "tripletex_generic_get", "tripletex_generic_post"
    }
}));

app.Run();
