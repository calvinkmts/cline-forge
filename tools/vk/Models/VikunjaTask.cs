using System.Text.Json.Serialization;

namespace Vk.Models;

public record VikunjaTask
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("bucket_id")]
    public long? BucketId { get; init; }

    [JsonPropertyName("project_id")]
    public long ProjectId { get; init; }

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; init; }
}

public record TaskCreateRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("project_id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public record TaskUpdateRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("done")]
    public bool? Done { get; set; }
}
