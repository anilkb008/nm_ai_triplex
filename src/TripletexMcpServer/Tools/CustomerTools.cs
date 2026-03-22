using System.ComponentModel;
using ModelContextProtocol.Server;
using TripletexMcpServer.Client;

namespace TripletexMcpServer.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool(Name = "search_customers"), Description("Search for customers in Tripletex.")]
    public static async Task<string> SearchCustomers(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Filter by name")] string? name = null,
        [Description("Filter by email")] string? email = null,
        [Description("Filter by organization number")] string? organizationNumber = null,
        [Description("Max results (default 50)")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/customer",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["name"] = name ?? "", ["email"] = email ?? "",
                ["organizationNumber"] = organizationNumber ?? ""
            });
    }

    [McpServerTool(Name = "get_customer"), Description("Get a specific customer by ID.")]
    public static async Task<string> GetCustomer(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Customer ID")] int id)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, $"/customer/{id}");
    }

    [McpServerTool(Name = "create_customer"), Description("Create a new customer.")]
    public static async Task<string> CreateCustomer(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Customer/company name")] string name,
        [Description("Email address")] string? email = null,
        [Description("Organization number (9 digits)")] string? organizationNumber = null,
        [Description("Phone number")] string? phoneNumber = null,
        [Description("Invoice email")] string? invoiceEmail = null,
        [Description("True if private individual (not a company)")] bool isPrivateIndividual = false)
    {
        var body = new Dictionary<string, object?> { ["name"] = name, ["isPrivateIndividual"] = isPrivateIndividual };
        if (email != null) body["email"] = email;
        if (organizationNumber != null) body["organizationNumber"] = organizationNumber;
        if (phoneNumber != null) body["phoneNumber"] = phoneNumber;
        if (invoiceEmail != null) body["invoiceEmail"] = invoiceEmail;

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/customer", body);
    }

    [McpServerTool(Name = "update_customer"), Description("Update an existing customer.")]
    public static async Task<string> UpdateCustomer(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Customer ID")] int id,
        [Description("Name")] string? name = null,
        [Description("Email")] string? email = null,
        [Description("Organization number")] string? organizationNumber = null,
        [Description("Phone number")] string? phoneNumber = null)
    {
        var body = new Dictionary<string, object?> { ["id"] = id };
        if (name != null) body["name"] = name;
        if (email != null) body["email"] = email;
        if (organizationNumber != null) body["organizationNumber"] = organizationNumber;
        if (phoneNumber != null) body["phoneNumber"] = phoneNumber;

        return await TripletexClient.PutAsync(baseUrl, sessionToken, $"/customer/{id}", body);
    }

    [McpServerTool(Name = "search_suppliers"), Description("Search for suppliers/vendors.")]
    public static async Task<string> SearchSuppliers(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Filter by name")] string? name = null,
        [Description("Filter by organization number")] string? organizationNumber = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/supplier",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["name"] = name ?? "", ["organizationNumber"] = organizationNumber ?? ""
            });
    }

    [McpServerTool(Name = "create_supplier"), Description("Create a new supplier.")]
    public static async Task<string> CreateSupplier(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Supplier name")] string name,
        [Description("Organization number")] string? organizationNumber = null,
        [Description("Email")] string? email = null,
        [Description("Phone number")] string? phoneNumber = null)
    {
        var body = new Dictionary<string, object?> { ["name"] = name };
        if (organizationNumber != null) body["organizationNumber"] = organizationNumber;
        if (email != null) body["email"] = email;
        if (phoneNumber != null) body["phoneNumber"] = phoneNumber;

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/supplier", body);
    }
}
