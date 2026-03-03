using System.CommandLine;
using Vk.Services;

namespace Vk.Commands;

public static class AuthCommand
{
    public static Command Create(VikunjaService vikunjaService)
    {
        var command = new Command("auth", "Ping the Vikunja API to check authentication status.");
        command.SetAction(async _ => await ExecuteAuthCheckAsync(vikunjaService));
        return command;
    }

    private static async Task ExecuteAuthCheckAsync(VikunjaService vikunjaService)
    {
        Console.WriteLine("Checking authentication...");

        try
        {
            var user = await vikunjaService.GetCurrentUserAsync();

            if (user is null)
            {
                DisplayAuthFailure();
                return;
            }

            DisplayAuthSuccess(user.Username);
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Network error: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void DisplayAuthSuccess(string? username)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Authentication successful! Connected as: {username}");
        Console.ResetColor();
    }

    private static void DisplayAuthFailure()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Authentication failed.");
        Console.ResetColor();
    }
}
