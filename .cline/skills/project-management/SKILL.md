---
name: project-management
description: Manage tasks, track progress, create epics, and organize project boards. Use this skill whenever the user asks to "create a task", "update my board", "check off a to-do", or manage project workflows. 
---

# Project & Task Management (Vikunja)

You are equipped with a single local CLI tool (`vk`) to manage projects, epics, and tasks. 

## 🏗️ HIERARCHY & EPICS
Vikunja does not have a native "Epic" entity. We simulate Epics using Sub-Projects.
**Hierarchy**: `Main Project` -> `Epic (Sub-Project)` -> `Tasks`.
* When creating an Epic, you are creating a Project and setting its `parent-project-id` to the Main Project.
* When creating a Task for an Epic, you set the task's `project-id` to the Epic's ID.

## 🛑 STRICT RULES (MANDATORY)
1. **NO MARKDOWN CHECKLISTS**: You are STRICTLY BANNED from creating, updating, or maintaining standalone `.md` file checklists for task tracking. All tasks MUST be managed using the `vk` CLI tool.
2. **NO MCP SERVERS**: You are STRICTLY BANNED from using any Model Context Protocol (MCP) servers for task management. Rely ONLY on the terminal binary.
3. **TERMINAL ONLY**: Execute these commands directly in your terminal workspace. Parse the JSON standard output to verify success.
4. **RICH DESCRIPTIONS**: You MUST use Markdown formatting inside the `--description` string (e.g., bullet points, code blocks, bold text) to keep task and epic details highly readable. Use standard shell escaping for multiline strings.

## 🛠️ CLI COMMANDS

### 1. `vk [command] [subcommand]`
The base command structure for all operations.

### 2. `vk auth`
Use this command to verify connectivity.

### 3. `vk project list`
Use this command to see all projects and their IDs.

### 4. `vk task list --project-id <id>`
Use this command to see tasks associated with a specific project ID.

### 5. `vk task create --project-id <id> --title "Task Name"`
Use this command to add new items to a project.

### 6. STOPS
Always check `vk project list` first if you don't know a Project ID.

### 7. BAN
You are FORBIDDEN from using local markdown files for task tracking; you MUST use the `vk` CLI.
