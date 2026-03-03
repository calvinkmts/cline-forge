using System.CommandLine;
using Vk.Models;
using Vk.Services;

namespace Vk.Commands;

public static class TaskCommand
{
    public static Command Create(VikunjaService vikunjaService)
    {
        var command = new Command("task", "Manage Vikunja tasks");

        command.Add(CreateListCommand(vikunjaService));
        command.Add(CreateGetCommand(vikunjaService));
        command.Add(CreateCreateCommand(vikunjaService));
        command.Add(CreateDoneCommand(vikunjaService));

        return command;
    }

    private static Command CreateListCommand(VikunjaService vikunjaService)
    {
        // Use object initializer for description because constructor only takes Name
        var projectIdArg = new Option<long>("--project-id") { Description = "The ID of the project to list tasks for" };
        var command = new Command("list", "List tasks for a project");
        command.Add(projectIdArg);

        command.SetAction(async context =>
        {
            // Use GetValueForArgument directly on the context
            var projectId = context.GetValue<long>("--project-id");
            Console.WriteLine($"🔍 Fetching tasks for project {projectId}...");

            var tasks = await vikunjaService.ListTasksAsync(projectId);

            if (tasks.Count == 0)
            {
                Console.WriteLine("No tasks found.");
                return;
            }

            foreach (var task in tasks)
            {
                Console.ForegroundColor = task.Done ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.Write($"[{task.Id}] [{(task.Done ? "DONE" : "TODO")}] ");
                Console.ResetColor();
                Console.WriteLine(task.Title);
            }
        });
        return command;
    }

    private static Command CreateGetCommand(VikunjaService vikunjaService)
    {
        var idArg = new Option<long>("--id") { Description = "The ID of the task to get" };
        var command = new Command("get", "Show task details");
        command.Add(idArg);

        command.SetAction(async context =>
        {
            var id = context.GetValue<long>("--id");
            var task = await vikunjaService.GetTaskAsync(id);

            if (task != null)
            {
                Console.WriteLine($"\n--- Task #{task.Id} ---");
                Console.WriteLine($"Title:       {task.Title}");
                Console.WriteLine($"Status:      {(task.Done ? "Completed" : "Pending")}");
                Console.WriteLine($"Description: {task.Description ?? "(Empty)"}");
            }
        });
        return command;
    }

    private static Command CreateCreateCommand(VikunjaService vikunjaService)
    {
        var projectIdOption = new Option<long>("--project-id") { Description = "The ID of the project to add the task to" };
        var titleOption = new Option<string>("--title") { Description = "The title of the task" };

        var command = new Command("create", "Add a new task");
        command.Add(projectIdOption);
        command.Add(titleOption);

        command.SetAction(async context =>
        {
            var projectId = context.GetValue<long>("--project-id");
            var title = context.GetValue<string>("--title");

            var result = await vikunjaService.CreateTaskAsync(projectId, new TaskCreateRequest { Title = title! });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Task created: {result?.Title} (ID: {result?.Id})");
            Console.ResetColor();
        });
        return command;
    }

    private static Command CreateDoneCommand(VikunjaService vikunjaService)
    {
        var idArg = new Argument<long>("--id") { Description = "The ID of the task to complete" };
        var command = new Command("done", "Mark a task as completed");
        command.Add(idArg);

        command.SetAction(async context =>
        {
            var id = context.GetValue<long>("--id");

            var updateRequest = new TaskUpdateRequest { Done = true };
            await vikunjaService.UpdateTaskAsync(id, updateRequest);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Task {id} marked as done.");
            Console.ResetColor();
        });
        return command;
    }
}