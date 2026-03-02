namespace Vk.Models;

public record VikunjaSettings
{
    public string ApiUrl { get; init; } = "http://localhost:3456/api/v1/";
    public string? ApiToken { get; init; }
}