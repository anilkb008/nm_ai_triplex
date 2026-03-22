using System.ComponentModel;
using ModelContextProtocol.Server;
using TripletexMcpServer.Client;

namespace TripletexMcpServer.Tools;

[McpServerToolType]
public static class ProjectTools
{
    [McpServerTool(Name = "search_projects"), Description("Search for projects.")]
    public static async Task<string> SearchProjects(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Filter by name")] string? name = null,
        [Description("Filter by number")] string? number = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/project",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["name"] = name ?? "", ["number"] = number ?? ""
            });
    }

    [McpServerTool(Name = "get_project"), Description("Get a specific project by ID.")]
    public static async Task<string> GetProject(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Project ID")] int id)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, $"/project/{id}");
    }

    [McpServerTool(Name = "create_project"), Description("Create a new project.")]
    public static async Task<string> CreateProject(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Project name")] string name,
        [Description("Project number (optional)")] string? number = null,
        [Description("Customer ID")] int? customerId = null,
        [Description("Start date YYYY-MM-DD")] string? startDate = null,
        [Description("End date YYYY-MM-DD")] string? endDate = null,
        [Description("Project manager employee ID")] int? projectManagerId = null,
        [Description("Project description")] string? description = null)
    {
        var body = new Dictionary<string, object?> { ["name"] = name };
        if (number != null) body["number"] = number;
        if (customerId.HasValue) body["customer"] = new { id = customerId.Value };
        if (startDate != null) body["startDate"] = startDate;
        if (endDate != null) body["endDate"] = endDate;
        if (projectManagerId.HasValue) body["projectManager"] = new { id = projectManagerId.Value };
        if (description != null) body["description"] = description;

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/project", body);
    }

    [McpServerTool(Name = "search_timesheet_entries"), Description("Search timesheet/hour registration entries.")]
    public static async Task<string> SearchTimesheetEntries(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Start date YYYY-MM-DD")] string? dateFrom = null,
        [Description("End date YYYY-MM-DD")] string? dateTo = null,
        [Description("Filter by employee ID")] string? employeeId = null,
        [Description("Filter by project ID")] string? projectId = null,
        [Description("Max results")] int count = 50)
    {
        return await TripletexClient.GetAsync(baseUrl, sessionToken, "/timesheet/entry",
            new Dictionary<string, string>
            {
                ["from"] = "0", ["count"] = count.ToString(),
                ["dateFrom"] = dateFrom ?? "", ["dateTo"] = dateTo ?? "",
                ["employeeId"] = employeeId ?? "", ["projectId"] = projectId ?? ""
            });
    }

    [McpServerTool(Name = "create_timesheet_entry"), Description("Register hours worked on a project.")]
    public static async Task<string> CreateTimesheetEntry(
        [Description("Tripletex API base URL")] string baseUrl,
        [Description("Session token")] string sessionToken,
        [Description("Employee ID")] int employeeId,
        [Description("Project ID")] int projectId,
        [Description("Date YYYY-MM-DD")] string date,
        [Description("Hours worked")] double hours,
        [Description("Activity ID (optional)")] int? activityId = null,
        [Description("Comment")] string? comment = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["employee"] = new { id = employeeId },
            ["project"] = new { id = projectId },
            ["date"] = date,
            ["hours"] = hours
        };
        if (activityId.HasValue) body["activity"] = new { id = activityId.Value };
        if (comment != null) body["comment"] = comment;

        return await TripletexClient.PostAsync(baseUrl, sessionToken, "/timesheet/entry", body);
    }
}
