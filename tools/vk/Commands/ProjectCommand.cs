using System.CommandLine;
using Vk.Models;
using Vk.Services;

namespace Vk.Commands;

public static class ProjectCommand
{
    public static Command Create(VikunjaService vikunjaService)
    {
        var command = new Command("project", "Manage Vikunja projects");

        command.Add(CreateListCommand(vikunjaService));
        command.Add(CreateGetCommand(vikunjaService));
        command.Add(CreateCreateCommand(vikunjaService));

        return command;
    }

    private static Command CreateListCommand(VikunjaService vikunjaService)
    {
        var command = new Command("list", "List all projects");
        command.SetAction(async _ =>
        {
            var projects = await vikunjaService.ListProjectsAsync();
            foreach (var project in projects)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{project.Id}] - ");
                Console.ResetColor();
                Console.WriteLine(project.Title);
            }
        });
        return command;
    }

    private static Command CreateGetCommand(VikunjaService vikunjaService)
    {
        var command = new Command("get", "Get a specific project by ID");
        var idOption = new Option<long>("--id") { Description = "The Project ID" };
        command.Add(idOption);
        command.SetAction(async cmd =>
        {
            var id = cmd.GetValue<long>("--id");
            var project = await vikunjaService.GetProjectAsync(id);
            if (project != null)
            {
                Console.WriteLine($"Project: {project.Title}\nDescription: {project.Description}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Project not found.");
                Console.ResetColor();
            }
        });
        return command;
    }

    private static Command CreateCreateCommand(VikunjaService vikunjaService)
    {
        var command = new Command("create", "Create a new project");
        var titleOption = new Option<string>("--title") { Description = "The title of the project" };
        var descriptionOption = new Option<string>("--description") { Description = "The description of the project" };
        var parentProjectIdOption = new Option<long?>("--parent-project-id") { Description = "Optional parent project ID" };

        command.Add(titleOption);
        command.Add(descriptionOption);
        command.Add(parentProjectIdOption);

        command.SetAction(async cmd =>
        {
            var title = cmd.GetValue<string>("--title");
            var description = cmd.GetValue<string>("--description");
            var parentProjectId = cmd.GetValue<long?>("--parent-project-id");
            var project = await vikunjaService.CreateProjectAsync(new ProjectCreateRequest { Title = title!, Description = description!, ParentProjectId = parentProjectId });
            if (project != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Project created with ID: {project.Id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Failed to create project.");
                Console.ResetColor();
            }
        });
        return command;
    }
}