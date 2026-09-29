# Coding Agent

An in-app coding agent — a separate app from the Personal Assistant (own backend, frontend,
port, and database), living as a sibling folder in the same repo. See the
[root README](../README.md) for why it's split out this way.

**Phase 1 (Workspace) is done.** Register a local project by absolute folder path, browse its
file tree, and read file contents — all sandboxed to the project root.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite (no router yet — single page) |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `coding_agent` |

```
coding-agent/
  backend/src/
    CodingAgent.Core/            entities, DTOs
    CodingAgent.Infrastructure/  EF Core, Workspace/ (PathGuard, WorkspaceService)
    CodingAgent.Api/             ProjectsController, Program.cs
  frontend/src/
    App.tsx                      project register/select + workspace layout
    FileTree.tsx                 lazy-loading directory tree
```

## Security model

Every file operation goes through `PathGuard.Resolve(root, relativePath)` in
`CodingAgent.Infrastructure/Workspace/PathGuard.cs`. It normalizes the project root once at
registration time and rejects anything that would escape it — `..` segments, absolute paths,
anything outside the root — before touching disk. `WorkspaceService` additionally caps reads
at 1 MB, rejects binary files, and skips noise directories (`.git`, `node_modules`, `bin`,
`obj`, `dist`, `build`, …) when listing.

Unregistering a project only deletes its database row — it never touches files on disk.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the Personal Assistant's — it's a
  separate database, `coding_agent`)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres coding_agent      # or: CREATE DATABASE coding_agent;
cd backend/src/CodingAgent.Api
dotnet user-secrets set "ConnectionStrings:CodingAgent" "Host=localhost;Port=5432;Database=coding_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
```

### 2. Run the backend

```bash
cd backend/src/CodingAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5078> — health check at `/api/health`.

To manage migrations manually:

```bash
$env:CODING_AGENT_DB = "Host=localhost;Port=5432;Database=coding_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/CodingAgent.Infrastructure --startup-project backend/src/CodingAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5174> (or click "Coding Agent ↗" in the Personal Assistant's sidebar).
`/api` is proxied to the backend (override with `VITE_API_TARGET`).

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| GET/POST | `/api/projects` | List / register a project |
| GET/DELETE | `/api/projects/{id}` | Open (bumps last-opened) / unregister |
| GET | `/api/projects/{id}/tree?path=` | Lazy directory listing, relative to project root |
| GET | `/api/projects/{id}/file?path=` | Read a text file's content, language, line count |

## Roadmap

- **2 — Code intelligence**: file/code search, project structure analysis, AI code
  explanation, error/log analysis, project context management
- **3 — Code modification**: generation, diff, user approval, apply, undo
- **4 — Real agent**: `ITool`/`IToolRegistry`/`ILlmProvider` (same pattern as the Personal
  Assistant), a planner, an execution loop, multi-step tasks, retry/failure handling
- **5 — Terminal & testing**: allowlisted commands, build/test, capture output, analyze
  failures, fix → retest loop
- **6 — Git**: status, diff, branch, commit, revert — with approval before commit
- **7 — GitHub**: auth, repo selection, clone, branch, push, PR creation
- **8 — Advanced autonomous agent**: debug → modify → test → analyze → repeat until green →
  prepare PR → ask for final approval
