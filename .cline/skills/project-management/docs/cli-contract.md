# Vikunja CLI Blueprint (C# .NET)

## 1. Global Architecture Requirements

* **Framework**: .NET 8/9 Console Application.
* **CLI Parser**: `System.CommandLine`. The root command is `vk`, containing subcommands `list`, `create`, and `update`.
* **Configuration**: The CLI MUST read `VIKUNJA_API_URL` and `VIKUNJA_TOKEN` from environment variables.
* **Authentication**: Include header `Authorization: Bearer <VIKUNJA_TOKEN>`.
* **AI-Friendly Output**: Standard output (`stdout`) MUST be strictly formatted JSON on success. Standard error (`stderr`) MUST contain plain text error messages on failure. Exit code `0` on success, non-zero on failure.
* **Entity Resolution**: The `--type` flag dictates whether the CLI interacts with Vikunja's `/projects` endpoints (Epics) or `/tasks` endpoints (Tasks).
* **Markdown Handling**: The `--description` argument will receive raw Markdown strings. Ensure `System.CommandLine` handles multiline strings properly and passes them unmodified in the JSON payload so Vikunja can render the Markdown natively.

## 2. Command Specifications

### 2.1. `vk list`

* **System.CommandLine Setup**: `Command("list")`
  * `Option<string>("--type")` -> Values: `task`, `epic`. Default: `task`.
  * `Option<int?>("--project-id", "-p")`
  * `Option<string>("--search", "-s")`
* **Epic Logic (`--type epic`)**:
  * API: `GET /api/v1/projects`
  * Filter Logic: If `--project-id` is provided, filter the returned JSON array in C# where `parent_project_id == {id}`.
  * Expected Output: Array of `EpicRecord`.
* **Task Logic (`--type task`)**:
  * API: `GET /api/v1/tasks`
  * Filter Logic: If `--project-id` is provided, append `filter=project_id={id}` to the query. If `--search` is provided, append `s={search}`.
  * Expected Output: Array of `TaskRecord`.

### 2.2. `vk create`

* **System.CommandLine Setup**: `Command("create")`
  * `Option<string>("--type")` -> Values: `task`, `epic`. Default: `task`.
  * `Option<int>("--project-id", "-p") { IsRequired = true }`
  * `Option<string>("--title", "-t") { IsRequired = true }`
  * `Option<string>("--description", "-d")`
* **Epic Logic (`--type epic`)**:
  * API: `PUT /api/v1/projects`
  * Payload: `{"title": "...", "description": "...", "parent_project_id": {project-id}}`
  * Expected Output: Single `EpicRecord`.
* **Task Logic (`--type task`)**:
  * API: `PUT /api/v1/projects/{project-id}/tasks`
  * Payload: `{"title": "...", "description": "..."}`
  * Expected Output: Single `TaskRecord`.

### 2.3. `vk update`

* **System.CommandLine Setup**: `Command("update")`
  * `Option<int>("--id", "-i") { IsRequired = true }`
  * `Option<string>("--type")` -> Values: `task`, `epic`. Default: `task`.
  * `Option<string>("--title", "-t")`
  * `Option<string>("--description", "-d")`
  * `Option<bool?>("--done")` (Ignored if epic)
  * `Option<double?>("--percent-done")` (Ignored if epic)
* **Epic Logic (`--type epic`)**:
  * API: `POST /api/v1/projects/{id}`
  * Payload: `{"title": "...", "description": "..."}` (omit nulls)
  * Expected Output: Single `EpicRecord`.
* **Task Logic (`--type task`)**:
  * API: `POST /api/v1/tasks/{id}`
  * Payload: `{"done": true, "title": "...", "description": "..."}` (omit nulls)
  * Expected Output: Single `TaskRecord`.

## 3. C# Data Contracts (Records)

Use `System.Text.Json` with `JsonIgnoreCondition.WhenWritingNull` for minimal payloads.

```csharp
using System.Text.Json.Serialization;

// Handles Sub-Projects (Epics)
public record EpicRecord
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("parent_project_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ParentProjectId { get; init; }
}

// Handles Tasks
public record TaskRecord
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("done")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Done { get; init; }

    [JsonPropertyName("project_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ProjectId { get; init; }

    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Priority { get; init; }

    [JsonPropertyName("percent_done")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PercentDone { get; init; }
}
```

## 4. Standard Output Format Example (Crucial for AI Parsing)

When `vk list --type epic --project-id 1` executes successfully, print exclusively to stdout:

```json
[
  {
    "id": 5,
    "title": "Auth Overhaul",
    "description": "## Specifications\n* Migrate from JWT to IdentityServer\n* Add RBAC rules",
    "parent_project_id": 1
  }
]
```
