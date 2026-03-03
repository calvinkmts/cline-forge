using System.Text.Json.Serialization;

namespace Vk.Models;

public record TaskComment
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("comment")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("author_id")]
    public long AuthorId { get; init; }

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; init; }
}

public record CommentCreateRequest
{
    [JsonPropertyName("comment")]
    public string Content { get; set; } = string.Empty;
}
