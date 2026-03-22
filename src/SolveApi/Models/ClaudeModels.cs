using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolveApi.Models;

// ── Request ──────────────────────────────────────────────────────────────────

public class ClaudeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-sonnet-4-6";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 8192;

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("tools")]
    public List<ClaudeTool>? Tools { get; set; }

    [JsonPropertyName("messages")]
    public List<ClaudeMessage> Messages { get; set; } = [];

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolChoice { get; set; }
}

public class ClaudeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    // Content can be a string or a list of content blocks
    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;
}

public class ClaudeTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public JsonElement InputSchema { get; set; }
}

// ── Content blocks ────────────────────────────────────────────────────────────

public class TextBlock
{
    [JsonPropertyName("type")]
    public string Type => "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class ImageBlock
{
    [JsonPropertyName("type")]
    public string Type => "image";

    [JsonPropertyName("source")]
    public ImageSource Source { get; set; } = new();
}

public class ImageSource
{
    [JsonPropertyName("type")]
    public string Type => "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class DocumentBlock
{
    [JsonPropertyName("type")]
    public string Type => "document";

    [JsonPropertyName("source")]
    public DocumentSource Source { get; set; } = new();
}

public class DocumentSource
{
    [JsonPropertyName("type")]
    public string Type => "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = "application/pdf";

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class ToolResultBlock
{
    [JsonPropertyName("type")]
    public string Type => "tool_result";

    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("is_error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsError { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────────────

public class ClaudeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<JsonElement> Content { get; set; } = [];

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("usage")]
    public ClaudeUsage? Usage { get; set; }

    [JsonPropertyName("error")]
    public ClaudeError? Error { get; set; }
}

public class ClaudeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

public class ClaudeError
{
    [JsonPropertyName("type")]
    public string ErrorType { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ToolUseInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonElement Input { get; set; }
}
