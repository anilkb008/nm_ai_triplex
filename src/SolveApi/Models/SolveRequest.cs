using System.Text.Json.Serialization;

namespace SolveApi.Models;

public class SolveRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<FileAttachment>? Files { get; set; }

    [JsonPropertyName("tripletex_credentials")]
    public TripletexCredentials TripletexCredentials { get; set; } = new();
}

public class FileAttachment
{
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class TripletexCredentials
{
    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("session_token")]
    public string SessionToken { get; set; } = string.Empty;
}

public class SolveResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "completed";
}
