using System.ComponentModel;
using ModelContextProtocol.Server;
using TripletexMcpServer.Client;

namespace TripletexMcpServer.Tools;

[McpServerToolType]
public static class EmployeeTools
{
    [McpServerTool(Name = "search_employees"), Description("Search for employees in Tripletex.")]
    public static async Task<string> SearchEmployees(
        [Description("Tripletex API base URL (e.g. https://xyz.tripletex.dev/v2)")] string baseUrl,
        [Description("Session token for authentication")] string sessionToken,
        [Description("Filter by first name")] string? firstName = null,
        [Description("Filter by last name")] string? lastName = null,
        [Description("Filter by email")] string? email = null,
        [Description("Max results (default 50)")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/employee",
            new Dictionary<string, string>
            {
                ["from"] = "0",
                ["count"] = count.ToString(),
                ["firstName"] = firstName ?? "",
                ["lastName"] = lastName ?? "",
                ["email"] = email ?? ""
            });
    }

    [McpServerTool(Name = "get_employee"), Description("Get a specific employee by ID.")]
    public static async Task<string> GetEmployee(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Employee ID")] int id)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, $"/employee/{id}");
    }

    [McpServerTool(Name = "create_employee"), Description("Create a new employee in Tripletex.")]
    public static async Task<string> CreateEmployee(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("First name")] string firstName,
        [Description("Last name")] string lastName,
        [Description("Email address")] string? email = null,
        [Description("Employee number")] string? employeeNumber = null,
        [Description("Date of birth YYYY-MM-DD")] string? dateOfBirth = null,
        [Description("Mobile phone number")] string? phoneNumberMobile = null,
        [Description("Bank account number")] string? bankAccountNumber = null,
        [Description("National identity number (personnummer)")] string? nationalIdentityNumber = null)
    {
        var body = new Dictionary<string, object?> { ["firstName"] = firstName, ["lastName"] = lastName };
        if (email != null) body["email"] = email;
        if (employeeNumber != null) body["employeeNumber"] = employeeNumber;
        if (dateOfBirth != null) body["dateOfBirth"] = dateOfBirth;
        if (phoneNumberMobile != null) body["phoneNumberMobile"] = phoneNumberMobile;
        if (bankAccountNumber != null) body["bankAccountNumber"] = bankAccountNumber;
        if (nationalIdentityNumber != null) body["nationalIdentityNumber"] = nationalIdentityNumber;

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/employee", body);
    }

    [McpServerTool(Name = "update_employee"), Description("Update an existing employee.")]
    public static async Task<string> UpdateEmployee(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Employee ID")] int id,
        [Description("First name")] string? firstName = null,
        [Description("Last name")] string? lastName = null,
        [Description("Email")] string? email = null,
        [Description("Mobile phone")] string? phoneNumberMobile = null,
        [Description("Bank account number")] string? bankAccountNumber = null)
    {
        var body = new Dictionary<string, object?> { ["id"] = id };
        if (firstName != null) body["firstName"] = firstName;
        if (lastName != null) body["lastName"] = lastName;
        if (email != null) body["email"] = email;
        if (phoneNumberMobile != null) body["phoneNumberMobile"] = phoneNumberMobile;
        if (bankAccountNumber != null) body["bankAccountNumber"] = bankAccountNumber;

        return await TripletexClient.PutAsync(baseUrl, sessionToken, $"/employee/{id}", body);
    }
}
