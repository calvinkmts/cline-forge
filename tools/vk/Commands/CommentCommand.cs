using System.CommandLine;
using Vk.Models;
using Vk.Services;

namespace Vk.Commands;

public static class CommentCommand
{
    public static Command Create(VikunjaService vikunjaService)
    {
        var command = new Command("comment", "Manage task comments");

        command.Add(CreateListCommand(vikunjaService));
        command.Add(CreateCreateCommand(vikunjaService));

        return command;
    }

    private static Command CreateListCommand(VikunjaService vikunjaService)
    {
        var command = new Command("list", "List all comments for a specific task");
        var taskIdOption = new Option<long>("--task-id") { Description = "The ID of the task to list comments for" };
        command.Add(taskIdOption);
        command.SetAction(async cmd =>
        {
            var taskId = cmd.GetValue<long>("--task-id");
            var comments = await vikunjaService.ListCommentsAsync(taskId);
            foreach (var comment in comments)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{comment.Id}] ");
                Console.ResetColor();
                Console.WriteLine(comment.Content);
            }
        });
        return command;
    }

    private static Command CreateCreateCommand(VikunjaService vikunjaService)
    {
        var command = new Command("create", "Create a new comment");
        var taskIdOption = new Option<long>("--task-id") { Description = "The ID of the task to add the comment to" };
        var contentOption = new Option<string>("--content") { Description = "The content of the comment" };
        command.Add(taskIdOption);
        command.Add(contentOption);
        command.SetAction(async cmd =>
        {
            var taskId = cmd.GetValue<long>("--task-id");
            var content = cmd.GetValue<string>("--content");
            var comment = await vikunjaService.CreateCommentAsync(taskId, new CommentCreateRequest { Content = content! });
            if (comment != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Comment created with ID: {comment.Id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Failed to create comment.");
                Console.ResetColor();
            }
        });
        return command;
    }
}