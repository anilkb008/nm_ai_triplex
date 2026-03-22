using System.Text.Json;
using SolveApi.Models;

namespace SolveApi.Tools;

/// <summary>
/// All Claude tool definitions for the Tripletex API.
/// Credentials are NOT in the tool schemas — they are injected by AgentService at execution time.
/// </summary>
public static class ToolDefinitions
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static ClaudeTool MakeTool(string name, string description, string schemaJson) =>
        new()
        {
            Name = name,
            Description = description,
            InputSchema = JsonDocument.Parse(schemaJson).RootElement
        };

    public static List<ClaudeTool> All { get; } =
    [
        // ── Company ──────────────────────────────────────────────────────────
        MakeTool("get_company_info",
            "Get information about the current company in Tripletex, including name, organization number, and settings.",
            """{"type":"object","properties":{},"required":[]}"""),

        MakeTool("get_accounts",
            "Get the chart of accounts (ledger accounts) for the company. Useful for finding account numbers for voucher postings.",
            """{"type":"object","properties":{"query":{"type":"string","description":"Optional search term to filter accounts by name or number"},"count":{"type":"integer","description":"Max results (default 100)"}},"required":[]}"""),

        MakeTool("get_currencies",
            "Get all available currencies. Returns currency codes and names.",
            """{"type":"object","properties":{},"required":[]}"""),

        MakeTool("get_vat_types",
            "Get all VAT types available for use in invoices and vouchers.",
            """{"type":"object","properties":{},"required":[]}"""),

        MakeTool("get_departments",
            "List all departments in the company.",
            """{"type":"object","properties":{},"required":[]}"""),

        MakeTool("create_department",
            "Create a new department.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Department name"},"departmentNumber":{"type":"string","description":"Optional department number/code"}},"required":["name"]}"""),

        // ── Employees ────────────────────────────────────────────────────────
        MakeTool("search_employees",
            "Search for employees. Returns a list of matching employees.",
            """{"type":"object","properties":{"firstName":{"type":"string","description":"Filter by first name"},"lastName":{"type":"string","description":"Filter by last name"},"email":{"type":"string","description":"Filter by email address"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_employee",
            "Get detailed information about a specific employee by their ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Employee ID"}},"required":["id"]}"""),

        MakeTool("create_employee",
            "Create a new employee in Tripletex.",
            """{"type":"object","properties":{"firstName":{"type":"string","description":"First name"},"lastName":{"type":"string","description":"Last name"},"email":{"type":"string","description":"Email address"},"employeeNumber":{"type":"string","description":"Employee number/ID (optional, auto-generated if omitted)"},"dateOfBirth":{"type":"string","description":"Date of birth in YYYY-MM-DD format"},"phoneNumberMobile":{"type":"string","description":"Mobile phone number"},"bankAccountNumber":{"type":"string","description":"Bank account number"},"nationalIdentityNumber":{"type":"string","description":"National identity number (personnummer)"},"iban":{"type":"string","description":"IBAN bank account number"},"bic":{"type":"string","description":"BIC/SWIFT code for bank"},"department":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Department reference by ID"}},"required":["firstName","lastName"]}"""),

        MakeTool("update_employee",
            "Update an existing employee's information.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Employee ID to update"},"firstName":{"type":"string"},"lastName":{"type":"string"},"email":{"type":"string"},"phoneNumberMobile":{"type":"string"},"bankAccountNumber":{"type":"string"},"dateOfBirth":{"type":"string","description":"YYYY-MM-DD"},"department":{"type":"object","properties":{"id":{"type":"integer"}}}},"required":["id"]}"""),

        // ── Customers ────────────────────────────────────────────────────────
        MakeTool("search_customers",
            "Search for customers. Returns a list of matching customers.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Filter by customer name"},"email":{"type":"string","description":"Filter by email"},"organizationNumber":{"type":"string","description":"Filter by organization number"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_customer",
            "Get detailed information about a specific customer by ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Customer ID"}},"required":["id"]}"""),

        MakeTool("create_customer",
            "Create a new customer in Tripletex.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Customer/company name"},"email":{"type":"string","description":"Email address"},"organizationNumber":{"type":"string","description":"Organization number (org.nr)"},"phoneNumber":{"type":"string","description":"Phone number"},"address":{"type":"object","properties":{"addressLine1":{"type":"string"},"addressLine2":{"type":"string"},"city":{"type":"string"},"postalCode":{"type":"string"},"country":{"type":"object","properties":{"id":{"type":"integer"}}}}},"currency":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Currency (e.g. {\"id\": 1} for NOK)"},"invoiceEmail":{"type":"string"},"isPrivateIndividual":{"type":"boolean","description":"True for private persons, false for companies"}},"required":["name"]}"""),

        MakeTool("update_customer",
            "Update an existing customer.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Customer ID"},"name":{"type":"string"},"email":{"type":"string"},"organizationNumber":{"type":"string"},"phoneNumber":{"type":"string"}},"required":["id"]}"""),

        // ── Suppliers ────────────────────────────────────────────────────────
        MakeTool("search_suppliers",
            "Search for suppliers/vendors.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Filter by supplier name"},"organizationNumber":{"type":"string","description":"Filter by organization number"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_supplier",
            "Get detailed information about a specific supplier by ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Supplier ID"}},"required":["id"]}"""),

        MakeTool("create_supplier",
            "Create a new supplier/vendor.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Supplier name"},"organizationNumber":{"type":"string","description":"Organization number"},"email":{"type":"string"},"phoneNumber":{"type":"string"},"address":{"type":"object","properties":{"addressLine1":{"type":"string"},"city":{"type":"string"},"postalCode":{"type":"string"},"country":{"type":"object","properties":{"id":{"type":"integer"}}}}}},"required":["name"]}"""),

        MakeTool("update_supplier",
            "Update an existing supplier.",
            """{"type":"object","properties":{"id":{"type":"integer"},"name":{"type":"string"},"organizationNumber":{"type":"string"},"email":{"type":"string"},"phoneNumber":{"type":"string"}},"required":["id"]}"""),

        // ── Products ─────────────────────────────────────────────────────────
        MakeTool("search_products",
            "Search for products/services.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Filter by product name"},"number":{"type":"string","description":"Filter by product number"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_product",
            "Get a specific product by ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Product ID"}},"required":["id"]}"""),

        MakeTool("create_product",
            "Create a new product or service.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Product name"},"number":{"type":"string","description":"Product number/SKU"},"costExcludingVatCurrency":{"type":"number","description":"Cost price excluding VAT"},"priceExcludingVatCurrency":{"type":"number","description":"Sales price excluding VAT"},"priceIncludingVatCurrency":{"type":"number","description":"Sales price including VAT"},"vatType":{"type":"object","properties":{"id":{"type":"integer"}},"description":"VAT type reference"},"unit":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Unit of measure reference"},"isStockItem":{"type":"boolean"},"description":{"type":"string"}},"required":["name"]}"""),

        MakeTool("update_product",
            "Update an existing product.",
            """{"type":"object","properties":{"id":{"type":"integer"},"name":{"type":"string"},"number":{"type":"string"},"priceExcludingVatCurrency":{"type":"number"},"priceIncludingVatCurrency":{"type":"number"}},"required":["id"]}"""),

        // ── Invoices ─────────────────────────────────────────────────────────
        MakeTool("search_invoices",
            "Search for invoices.",
            """{"type":"object","properties":{"invoiceDateFrom":{"type":"string","description":"Start date filter YYYY-MM-DD"},"invoiceDateTo":{"type":"string","description":"End date filter YYYY-MM-DD"},"customerId":{"type":"string","description":"Filter by customer ID"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_invoice",
            "Get detailed information about a specific invoice.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Invoice ID"}},"required":["id"]}"""),

        MakeTool("create_invoice",
            "Create a new customer invoice. Include invoice lines with products/services.",
            """{"type":"object","properties":{"customer":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Customer reference"},"invoiceDate":{"type":"string","description":"Invoice date YYYY-MM-DD"},"dueDate":{"type":"string","description":"Payment due date YYYY-MM-DD"},"comment":{"type":"string","description":"Invoice comment/notes"},"currency":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Currency (default NOK)"},"orders":{"type":"array","items":{"type":"object","properties":{"id":{"type":"integer"}}},"description":"Order references to invoice"},"invoiceLines":{"type":"array","description":"Invoice line items","items":{"type":"object","properties":{"product":{"type":"object","properties":{"id":{"type":"integer"}}},"description":{"type":"string"},"quantity":{"type":"number"},"unitPriceExcludingVatCurrency":{"type":"number"},"vatType":{"type":"object","properties":{"id":{"type":"integer"}}}}}}},"required":["customer","invoiceDate"]}"""),

        MakeTool("send_invoice",
            "Send an invoice to the customer by email or other method.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Invoice ID to send"},"sendType":{"type":"string","description":"Send method: EMAIL, EHF, AVTALEGIRO, VIPPS","enum":["EMAIL","EHF","AVTALEGIRO","VIPPS"]}},"required":["id"]}"""),

        // ── Orders ───────────────────────────────────────────────────────────
        MakeTool("search_orders",
            "Search for orders.",
            """{"type":"object","properties":{"customerId":{"type":"string","description":"Filter by customer ID"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("create_order",
            "Create a new sales order.",
            """{"type":"object","properties":{"customer":{"type":"object","properties":{"id":{"type":"integer"}}},"orderDate":{"type":"string","description":"Order date YYYY-MM-DD"},"deliveryDate":{"type":"string","description":"Delivery date YYYY-MM-DD"},"comment":{"type":"string"},"orderLines":{"type":"array","items":{"type":"object","properties":{"product":{"type":"object","properties":{"id":{"type":"integer"}}},"description":{"type":"string"},"quantity":{"type":"number"},"unitPriceExcludingVatCurrency":{"type":"number"},"vatType":"object"}}}},"required":["customer","orderDate"]}"""),

        // ── Projects ─────────────────────────────────────────────────────────
        MakeTool("search_projects",
            "Search for projects.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Filter by project name"},"number":{"type":"string","description":"Filter by project number"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_project",
            "Get a specific project by ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Project ID"}},"required":["id"]}"""),

        MakeTool("create_project",
            "Create a new project.",
            """{"type":"object","properties":{"name":{"type":"string","description":"Project name"},"number":{"type":"string","description":"Project number (optional)"},"customer":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Customer reference"},"startDate":{"type":"string","description":"Start date YYYY-MM-DD"},"endDate":{"type":"string","description":"End date YYYY-MM-DD"},"description":{"type":"string"},"projectManager":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Project manager employee reference"},"isOffer":{"type":"boolean","description":"True if this is an offer/quote"},"fixedprice":{"type":"number","description":"Fixed price for the project"},"currency":{"type":"object","properties":{"id":{"type":"integer"}}}},"required":["name"]}"""),

        MakeTool("update_project",
            "Update an existing project.",
            """{"type":"object","properties":{"id":{"type":"integer"},"name":{"type":"string"},"endDate":{"type":"string"},"description":{"type":"string"}},"required":["id"]}"""),

        // ── Timesheet ────────────────────────────────────────────────────────
        MakeTool("search_timesheet_entries",
            "Search for timesheet / hour registration entries.",
            """{"type":"object","properties":{"dateFrom":{"type":"string","description":"Start date YYYY-MM-DD"},"dateTo":{"type":"string","description":"End date YYYY-MM-DD"},"employeeId":{"type":"string","description":"Filter by employee ID"},"projectId":{"type":"string","description":"Filter by project ID"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("create_timesheet_entry",
            "Register hours for an employee on a project (timesheet entry).",
            """{"type":"object","properties":{"employee":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Employee reference"},"project":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Project reference"},"activity":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Activity type reference"},"date":{"type":"string","description":"Date YYYY-MM-DD"},"hours":{"type":"number","description":"Number of hours worked"},"comment":{"type":"string","description":"Optional comment"}},"required":["employee","project","date","hours"]}"""),

        MakeTool("update_timesheet_entry",
            "Update an existing timesheet entry.",
            """{"type":"object","properties":{"id":{"type":"integer"},"hours":{"type":"number"},"date":{"type":"string"},"comment":{"type":"string"}},"required":["id"]}"""),

        // ── Ledger / Vouchers ─────────────────────────────────────────────────
        MakeTool("search_vouchers",
            "Search for accounting vouchers in the ledger.",
            """{"type":"object","properties":{"dateFrom":{"type":"string","description":"Start date YYYY-MM-DD"},"dateTo":{"type":"string","description":"End date YYYY-MM-DD"},"count":{"type":"integer","description":"Max results (default 50)"}},"required":[]}"""),

        MakeTool("get_voucher",
            "Get a specific voucher by ID.",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Voucher ID"}},"required":["id"]}"""),

        MakeTool("create_voucher",
            "Create a manual accounting voucher (bilag) with debit/credit postings. Debits and credits must balance.",
            """{"type":"object","properties":{"date":{"type":"string","description":"Voucher date YYYY-MM-DD"},"description":{"type":"string","description":"Voucher description"},"voucher_type":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Voucher type reference (optional)"},"postings":{"type":"array","description":"List of debit/credit postings (must balance)","items":{"type":"object","properties":{"account":{"type":"object","properties":{"id":{"type":"integer"}},"description":"Account reference"},"amountGrossCurrency":{"type":"number","description":"Amount (positive for debit, negative for credit)"},"currency":{"type":"object","properties":{"id":{"type":"integer"}}},"description":{"type":"string"},"vatType":{"type":"object","properties":{"id":{"type":"integer"}}}}}}},"required":["date","postings"]}"""),

        // ── Ledger postings ───────────────────────────────────────────────────
        MakeTool("search_postings",
            "Search ledger postings (transactions).",
            """{"type":"object","properties":{"dateFrom":{"type":"string","description":"Start date YYYY-MM-DD"},"dateTo":{"type":"string","description":"End date YYYY-MM-DD"},"count":{"type":"integer","description":"Max results (default 100)"}},"required":[]}"""),

        // ── Generic fallback ─────────────────────────────────────────────────
        MakeTool("tripletex_get",
            "Make a generic GET request to any Tripletex API endpoint. Use this when no specific tool is available.",
            """{"type":"object","properties":{"endpoint":{"type":"string","description":"API endpoint path, e.g. /employee or /project/1234"},"params":{"type":"object","description":"Optional query parameters as key-value pairs","additionalProperties":{"type":"string"}}},"required":["endpoint"]}"""),

        MakeTool("tripletex_post",
            "Make a generic POST request to any Tripletex API endpoint. Use when no specific tool is available.",
            """{"type":"object","properties":{"endpoint":{"type":"string","description":"API endpoint path"},"body":{"type":"object","description":"Request body as JSON object"}},"required":["endpoint","body"]}"""),
    ];
}
