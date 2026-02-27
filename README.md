# 🛠️ cline-forge

**A local-first workshop for AI-driven task management and experimental workflows.**

## 🎯 Vision

`cline-forge` is designed to act as a resilient local execution environment for agentic AI assistants (like Cline).

Traditional Model Context Protocol (MCP) servers often fail in heavily restricted corporate environments due to aggressive egress/ingress firewall rules. `cline-forge` solves this by providing a **Universal CLI Fallback Architecture**. It pairs a robust local task manager with polyglot CLI tools, allowing the AI to interact with projects simply by executing standard terminal commands.

## 🏗️ Architecture

1. **The Infrastructure (Docker):**

* **[Vikunja](https://vikunja.io/):** The core task and project management backend (running locally via SQLite).
* *Note: Database volumes are strictly git-ignored to ensure data remains completely isolated between different physical machines (e.g., Office vs. Home).*

1. **The Tooling (Polyglot CLI):**

* A language-agnostic directory for writing CLI utilities (Bash, Python, .NET, Go, etc.) that bridge the AI and the local REST APIs.
* Designed to be added to the system `$PATH` (`~/.bashrc`) so they can be invoked globally from any directory.

## 📂 Repository Structure

```text
cline-forge/
├── .clinerules                  # Global AI Persona & strict tooling directives
├── infrastructure/              # Docker compose and isolated database volumes
│   ├── docker-compose.yml       
│   └── volumes/                 # ⚠️ DO NOT COMMIT (SQLite data)
├── tools/                       # Polyglot system utilities
│   ├── src/                     # Source code for scripts/tools
│   └── bin/                     # Executables and wrappers (Add to $PATH)
└── mcp/                         # Experimental zone for home/unrestricted networks

```

## 🚀 Getting Started

### 1. Boot the Infrastructure

Ensure Docker is installed, then spin up the local backend:

```bash
cd infrastructure
docker compose up -d

```

* Vikunja UI: `http://localhost:3456`

### 2. Configure Global Tooling

Add the `bin` directory to your system path to allow global AI execution:

```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:/path/to/cline-forge/tools/bin"

```

### 3. Link the AI Rules

Point your AI assistant's global custom instructions to the `.clinerules` file in this repository to enforce the usage of your local CLI tools.
