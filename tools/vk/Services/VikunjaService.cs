using System.Net.Http.Json;
using Vk.Models;

namespace Vk.Services;

public class VikunjaService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<User?> GetCurrentUserAsync()
    {
        return await _httpClient.GetFromJsonAsync<User>("user");
    }

    public async Task<List<Project>> ListProjectsAsync()
    {
        var projects = await _httpClient.GetFromJsonAsync<List<Project>>("projects");
        return projects ?? [];
    }

    public async Task<Project?> GetProjectAsync(long id)
    {
        return await _httpClient.GetFromJsonAsync<Project>($"projects/{id}");
    }

    public async Task<Project?> CreateProjectAsync(ProjectCreateRequest model)
    {
        // Ensure this is POST and the string is exactly "projects"
        var response = await _httpClient.PutAsJsonAsync("projects", model);

        // This will help us debug if it fails again
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error: {error}");
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<Project>();
    }

    public async Task<List<VikunjaTask>> ListTasksAsync(long projectId)
    {
        var tasks = await _httpClient.GetFromJsonAsync<List<VikunjaTask>>($"projects/{projectId}/tasks");
        return tasks ?? [];
    }
    public async Task<VikunjaTask?> GetTaskAsync(long id)
    {
        return await _httpClient.GetFromJsonAsync<VikunjaTask>($"tasks/{id}");
    }

    public async Task<VikunjaTask?> CreateTaskAsync(long projectId, TaskCreateRequest model)
    {
        model.ProjectId = projectId;
        var response = await _httpClient.PutAsJsonAsync($"projects/{projectId}/tasks", model);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VikunjaTask>();
    }

    public async Task<VikunjaTask?> UpdateTaskAsync(long id, TaskUpdateRequest model)
    {
        var response = await _httpClient.PostAsJsonAsync($"tasks/{id}", model);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VikunjaTask>();
    }

    public async Task<List<TaskComment>> ListCommentsAsync(long taskId)
    {
        var comments = await _httpClient.GetFromJsonAsync<List<TaskComment>>($"tasks/{taskId}/comments");
        return comments ?? [];
    }

    public async Task<TaskComment?> CreateCommentAsync(long taskId, CommentCreateRequest model)
    {
        var response = await _httpClient.PutAsJsonAsync($"tasks/{taskId}/comments", model);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskComment>();
    }
}
