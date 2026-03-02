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

### 1. `vk list`
Retrieves a list of tasks or epics.
* **Usage**: `vk list [options]`
* **Options**: 
  * `--type <task|epic>` (Default: task)
  * `--search <s>` (Search by text)
  * `--project-id <id>` (For tasks: the epic/project ID. For epics: the parent project ID).
* **Examples**:
  * `vk list --type epic --project-id 1` (Lists all epics under Main Project 1)
  * `vk list --type task --project-id 5` (Lists all tasks under Epic 5)

### 2. `vk create`
Creates a new task or epic. 
* **Usage**: `vk create --type <task|epic> --title "<title>" [options]`
* **Options**:
  * `--project-id <id>` (Required. If type=epic, this is the parent project ID. If type=task, this is the Epic ID).
  * `--description "<desc>"` (Accepts full Markdown formatting)
  * `--priority <int>` (Tasks only)
* **Examples**:
  * `vk create --type epic --title "Auth Overhaul" --project-id 1 --description "## Goals\n- Migrate JWT\n- Add OAuth"`
  * `vk create --type task --title "Implement JWT" --project-id 5`

### 3. `vk update`
Updates an existing task or epic.
* **Usage**: `vk update --id <id> --type <task|epic> [options]`
* **Options**:
  * `--title "<new title>"`
  * `--description "<new desc>"` (Accepts full Markdown formatting)
  * `--done <true|false>` (Tasks only)
  * `--percent-done <float>` (Tasks only)
* **Examples**:
  * `vk update --type task --id 45 --done true`
  * `vk update --type epic --id 5 --description "Updated specs:\n\n* Need to include refresh tokens."`
