using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SolveApi.Services;

/// <summary>
/// Thin HTTP client for the Tripletex API.
/// Auth: Basic base64("0:{session_token}") per Tripletex convention.
/// </summary>
public class TripletexApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public TripletexApiClient(string baseUrl, string sessionToken)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(30);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"0:{sessionToken}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Generic HTTP helpers ──────────────────────────────────────────────────

    private async Task<string> GetAsync(string path, Dictionary<string, string>? query = null)
    {
        var url = BuildUrl(path, query);
        var response = await _http.GetAsync(url);
        return await ReadResponseAsync(response);
    }

    private async Task<string> PostAsync(string path, object body)
    {
        var url = BuildUrl(path);
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        return await ReadResponseAsync(response);
    }

    private async Task<string> PutAsync(string path, object body, Dictionary<string, string>? query = null)
    {
        var url = BuildUrl(path, query);
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(url, content);
        return await ReadResponseAsync(response);
    }

    private string BuildUrl(string path, Dictionary<string, string>? query = null)
    {
        var url = $"{_baseUrl}/{path.TrimStart('/')}";
        if (query is { Count: > 0 })
        {
            var qs = string.Join("&", query
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            if (!string.IsNullOrEmpty(qs))
                url += "?" + qs;
        }
        return url;
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                statusCode = (int)response.StatusCode,
                message = $"HTTP {(int)response.StatusCode}: {body}"
            });
        }
        return body;
    }

    // ── Company ───────────────────────────────────────────────────────────────

    public Task<string> GetCompanyInfoAsync() =>
        GetAsync("/company");

    // ── Chart of Accounts ─────────────────────────────────────────────────────

    public Task<string> GetAccountsAsync(string? query = null, int from = 0, int count = 100) =>
        GetAsync("/ledger/account", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["fields"] = "id,number,name,type,vatType"
        });

    // ── Currencies ────────────────────────────────────────────────────────────

    public Task<string> GetCurrenciesAsync() =>
        GetAsync("/currency", new Dictionary<string, string> { ["count"] = "200" });

    // ── Departments ───────────────────────────────────────────────────────────

    public Task<string> GetDepartmentsAsync() =>
        GetAsync("/department", new Dictionary<string, string> { ["count"] = "100" });

    public Task<string> CreateDepartmentAsync(object department) =>
        PostAsync("/department", department);

    // ── Employees ────────────────────────────────────────────────────────────

    public Task<string> SearchEmployeesAsync(string? firstName = null, string? lastName = null,
        string? email = null, int from = 0, int count = 50) =>
        GetAsync("/employee", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["firstName"] = firstName ?? "",
            ["lastName"] = lastName ?? "",
            ["email"] = email ?? ""
        });

    public Task<string> GetEmployeeAsync(int id) =>
        GetAsync($"/employee/{id}", new Dictionary<string, string>
        {
            ["fields"] = "id,firstName,lastName,email,employeeNumber,phoneNumberMobile,dateOfBirth,bankAccountNumber"
        });

    public Task<string> CreateEmployeeAsync(object employee) =>
        PostAsync("/employee", employee);

    public Task<string> UpdateEmployeeAsync(int id, object employee) =>
        PutAsync($"/employee/{id}", employee);

    // ── Customers ────────────────────────────────────────────────────────────

    public Task<string> SearchCustomersAsync(string? name = null, string? email = null,
        string? organizationNumber = null, int from = 0, int count = 50) =>
        GetAsync("/customer", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["name"] = name ?? "",
            ["email"] = email ?? "",
            ["organizationNumber"] = organizationNumber ?? ""
        });

    public Task<string> GetCustomerAsync(int id) =>
        GetAsync($"/customer/{id}");

    public Task<string> CreateCustomerAsync(object customer) =>
        PostAsync("/customer", customer);

    public Task<string> UpdateCustomerAsync(int id, object customer) =>
        PutAsync($"/customer/{id}", customer);

    // ── Suppliers ────────────────────────────────────────────────────────────

    public Task<string> SearchSuppliersAsync(string? name = null, string? organizationNumber = null,
        int from = 0, int count = 50) =>
        GetAsync("/supplier", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["name"] = name ?? "",
            ["organizationNumber"] = organizationNumber ?? ""
        });

    public Task<string> GetSupplierAsync(int id) =>
        GetAsync($"/supplier/{id}");

    public Task<string> CreateSupplierAsync(object supplier) =>
        PostAsync("/supplier", supplier);

    public Task<string> UpdateSupplierAsync(int id, object supplier) =>
        PutAsync($"/supplier/{id}", supplier);

    // ── Products ─────────────────────────────────────────────────────────────

    public Task<string> SearchProductsAsync(string? name = null, string? number = null,
        int from = 0, int count = 50) =>
        GetAsync("/product", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["name"] = name ?? "",
            ["number"] = number ?? ""
        });

    public Task<string> GetProductAsync(int id) =>
        GetAsync($"/product/{id}");

    public Task<string> CreateProductAsync(object product) =>
        PostAsync("/product", product);

    public Task<string> UpdateProductAsync(int id, object product) =>
        PutAsync($"/product/{id}", product);

    // ── Invoices ─────────────────────────────────────────────────────────────

    public Task<string> SearchInvoicesAsync(string? invoiceDateFrom = null,
        string? invoiceDateTo = null, string? customerId = null,
        int from = 0, int count = 50) =>
        GetAsync("/invoice", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["invoiceDateFrom"] = invoiceDateFrom ?? "",
            ["invoiceDateTo"] = invoiceDateTo ?? "",
            ["customerId"] = customerId ?? ""
        });

    public Task<string> GetInvoiceAsync(int id) =>
        GetAsync($"/invoice/{id}");

    public Task<string> CreateInvoiceAsync(object invoice) =>
        PostAsync("/invoice", invoice);

    public Task<string> SendInvoiceAsync(int id, string sendType = "EMAIL") =>
        PutAsync($"/invoice/{id}/:send", new { }, new Dictionary<string, string>
        {
            ["sendType"] = sendType
        });

    // ── Orders ───────────────────────────────────────────────────────────────

    public Task<string> SearchOrdersAsync(string? customerId = null, int from = 0, int count = 50) =>
        GetAsync("/order", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["customerId"] = customerId ?? ""
        });

    public Task<string> CreateOrderAsync(object order) =>
        PostAsync("/order", order);

    // ── Projects ─────────────────────────────────────────────────────────────

    public Task<string> SearchProjectsAsync(string? name = null, string? number = null,
        int from = 0, int count = 50) =>
        GetAsync("/project", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["name"] = name ?? "",
            ["number"] = number ?? ""
        });

    public Task<string> GetProjectAsync(int id) =>
        GetAsync($"/project/{id}");

    public Task<string> CreateProjectAsync(object project) =>
        PostAsync("/project", project);

    public Task<string> UpdateProjectAsync(int id, object project) =>
        PutAsync($"/project/{id}", project);

    // ── Timesheet ────────────────────────────────────────────────────────────

    public Task<string> SearchTimesheetEntriesAsync(string? dateFrom = null,
        string? dateTo = null, string? employeeId = null, string? projectId = null,
        int from = 0, int count = 50) =>
        GetAsync("/timesheet/entry", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["dateFrom"] = dateFrom ?? "",
            ["dateTo"] = dateTo ?? "",
            ["employeeId"] = employeeId ?? "",
            ["projectId"] = projectId ?? ""
        });

    public Task<string> CreateTimesheetEntryAsync(object entry) =>
        PostAsync("/timesheet/entry", entry);

    public Task<string> UpdateTimesheetEntryAsync(int id, object entry) =>
        PutAsync($"/timesheet/entry/{id}", entry);

    // ── Vouchers (ledger entries) ──────────────────────────────────────────

    public Task<string> SearchVouchersAsync(string? dateFrom = null,
        string? dateTo = null, int from = 0, int count = 50) =>
        GetAsync("/ledger/voucher", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["dateFrom"] = dateFrom ?? "",
            ["dateTo"] = dateTo ?? ""
        });

    public Task<string> GetVoucherAsync(int id) =>
        GetAsync($"/ledger/voucher/{id}");

    public Task<string> CreateVoucherAsync(object voucher) =>
        PostAsync("/ledger/voucher", voucher);

    // ── Posting (ledger posting) ───────────────────────────────────────────

    public Task<string> SearchPostingsAsync(string? dateFrom = null,
        string? dateTo = null, int from = 0, int count = 100) =>
        GetAsync("/ledger", new Dictionary<string, string>
        {
            ["from"] = from.ToString(),
            ["count"] = count.ToString(),
            ["dateFrom"] = dateFrom ?? "",
            ["dateTo"] = dateTo ?? ""
        });

    // ── VAT types ─────────────────────────────────────────────────────────

    public Task<string> GetVatTypesAsync() =>
        GetAsync("/vat");

    // ── Generic fallback ──────────────────────────────────────────────────

    public Task<string> GenericGetAsync(string endpoint, Dictionary<string, string>? query = null) =>
        GetAsync(endpoint, query);

    public Task<string> GenericPostAsync(string endpoint, object body) =>
        PostAsync(endpoint, body);

    public void Dispose() => _http.Dispose();
}
