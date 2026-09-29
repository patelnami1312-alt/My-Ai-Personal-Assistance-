# Home Management Agent

A real chat-driven home management assistant — a separate app from the Personal Assistant,
Coding Agent, and Fitness Agent (own backend, frontend, port, and database), living as a
sibling folder in the same repo. See the [root README](../README.md) for why it's split out
this way.

Track chores, home maintenance schedules, and household inventory/supplies by talking to it
(text or voice); it also has direct CRUD pages for each, plus a dashboard.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `home_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the other apps |

```
home-agent/
  backend/src/
    HomeAgent.Core/            entities, LLM contracts, abstractions, DTOs
    HomeAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    HomeAgent.Api/             7 controllers, Program.cs
  frontend/src/
    pages/                     Dashboard, Chat, Chores, Maintenance, Inventory
    components/Layout.tsx      sidebar
    hooks/useVoice.ts          same voice hook as the other apps
```

### How a chat turn flows

Same bounded tool-use loop pattern as the other agents: `POST /api/chat` -> `OrchestratorService`
builds a home-management system prompt -> loops against `ILlmProvider` with the tool list ->
`ToolRegistry` dispatches to `ITool` implementations -> PostgreSQL. Destructive tools
(`delete_*`) require `confirmed=true`. Completing a recurring chore automatically creates its
next occurrence, advanced by the chore's frequency.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps - separate database)
- An OpenAI API key (can reuse the same key as the other apps)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres home_agent      # or: CREATE DATABASE home_agent;
cd backend/src/HomeAgent.Api
dotnet user-secrets set "ConnectionStrings:HomeAgent" "Host=localhost;Port=5432;Database=home_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/HomeAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5081> - health check at `/api/health`.

To manage migrations manually:

```bash
$env:HOME_AGENT_DB = "Host=localhost;Port=5432;Database=home_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/HomeAgent.Infrastructure --startup-project backend/src/HomeAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5177>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Chores due today/overdue, low inventory count, upcoming maintenance, recent open chores |
| GET/POST/PUT/DELETE | `/api/chores[/{id}]` | Chore log (PUT updates status, fields, or both) |
| GET/POST/PUT/DELETE | `/api/maintenance[/{id}]` | Maintenance items (`PUT .../{id}/mark-done` marks done today) |
| GET/POST/PUT/DELETE | `/api/inventory[/{id}]` | Household inventory / supplies |

## Tools the assistant can call

`add_chore` · `get_chores` · `complete_chore` · `delete_chore*` ·
`add_maintenance_item` · `get_maintenance_items` · `mark_maintenance_done` · `delete_maintenance_item*` ·
`add_inventory_item` · `get_inventory` · `update_inventory_quantity` · `delete_inventory_item*` ·
`current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Push notifications / reminders for chores due soon or low stock (dashboard only, no alerts)
- Shopping-list generation from low inventory items
- Multi-user assignment tracking (assignedTo is a free-text field, not a real user account)
