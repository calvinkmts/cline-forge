using System.CommandLine;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Vk.Commands;
using Vk.Models;
using Vk.Services;

namespace Vk;

public static class Program
{
    private const string AppName = "Cline Forge: CLI for Vikunja Task Management";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var settings = LoadConfiguration();
            if (settings is null)
            {
                return (int)ExitCode.ConfigurationError;
            }

            using var httpClient = CreateHttpClient(settings);
            var vikunjaService = new VikunjaService(httpClient);
            var rootCommand = BuildRootCommand(vikunjaService);

            return await rootCommand.Parse(args).InvokeAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.ResetColor();
            return (int)ExitCode.UnexpectedError;
        }
    }

    private static VikunjaSettings? LoadConfiguration()
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configPath = Path.Combine(homePath, ".config", "cline-forge", "config.json");

        if (!File.Exists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Config file does not exist: {configPath}");
            Console.ResetColor();
            return null;
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var settings = config.GetSection("Vikunja").Get<VikunjaSettings>();

        if (string.IsNullOrWhiteSpace(settings?.ApiToken))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ API token is not set in the config file.");
            Console.ResetColor();
            return null;
        }

        return settings;
    }

    private static HttpClient CreateHttpClient(VikunjaSettings settings)
    {
        var httpClient = new HttpClient();

        httpClient.BaseAddress = new Uri(settings.ApiUrl.EndsWith('/') ? settings.ApiUrl : settings.ApiUrl + "/");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }

    private static RootCommand BuildRootCommand(VikunjaService vikunjaService)
    {
        var rootCommand = new RootCommand(AppName);
        rootCommand.Add(AuthCommand.Create(vikunjaService));
        rootCommand.Add(ProjectCommand.Create(vikunjaService));
        rootCommand.Add(TaskCommand.Create(vikunjaService));
        rootCommand.Add(CommentCommand.Create(vikunjaService));
        return rootCommand;
    }
}

public enum ExitCode
{
    Success = 0,
    ConfigurationError = 1,
    UnexpectedError = 2
}