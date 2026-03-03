using System.Text.Json.Serialization;

namespace Vk.Models;

public record Project
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; init; }
}

public record ProjectCreateRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_project_id")]
    public long? ParentProjectId { get; init; }
}
