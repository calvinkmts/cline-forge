using System.Text.Json.Serialization;

namespace Vk.Models;

public record User
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; init; }
}
