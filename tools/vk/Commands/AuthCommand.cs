using System.CommandLine;
using System.Text.Json;

namespace Vk.Commands;

public static class AuthCommand
{
    public static Command Create(HttpClient httpClient)
    {
        var command = new Command("auth", "Ping the Vikunja API to check authentication status.");
        command.SetAction(async _ => await ExecuteAuthCheckAsync(httpClient));
        return command;
    }

    private static async Task ExecuteAuthCheckAsync(HttpClient httpClient)
    {
        Console.WriteLine("Checking authentication...");

        try
        {
            var response = await httpClient.GetAsync("user");

            if (!response.IsSuccessStatusCode)
            {
                DisplayAuthFailure(response);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            // This is no longer 'async' because we already downloaded the string content
            HandleSuccessResponse(content, response.RequestMessage?.RequestUri);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Network error: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void HandleSuccessResponse(string content, Uri? requestUri)
    {
        try
        {
            var username = ExtractUsername(content);
            DisplayAuthSuccess(username);
        }
        catch (JsonException)
        {
            DisplayJsonWarning(content, requestUri);
        }
    }

    private static string? ExtractUsername(string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        return doc.RootElement.GetProperty("username").GetString();
    }

    private static void DisplayAuthSuccess(string? username)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Authentication successful! Connected as: {username}");
        Console.ResetColor();
    }

    private static void DisplayJsonWarning(string content, Uri? requestUri)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️ WARNING: The server returned HTML instead of JSON.");
        Console.WriteLine($"👉 Actual URL requested: {requestUri}");
        Console.WriteLine($"👉 Content Preview: {content[..Math.Min(150, content.Length)]}...");
        Console.ResetColor();
    }

    private static void DisplayAuthFailure(HttpResponseMessage response)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Authentication failed.");
        Console.WriteLine($"Server returned: {(int)response.StatusCode} {response.ReasonPhrase}");
        Console.ResetColor();
    }
}