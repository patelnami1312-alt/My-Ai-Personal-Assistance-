# Shopping Agent

A real chat-driven shopping list assistant — a separate app from the Personal Assistant, Coding
Agent, and Fitness Agent (own backend, frontend, port, and database), living as a sibling folder
in the same repo. See the [root README](../README.md) for why it's split out this way.

Build and manage a shopping list by talking to it (text or voice); it also has direct CRUD pages
for the list and purchase history, plus a dashboard.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `shopping_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the other apps |

```
shopping-agent/
  backend/src/
    ShoppingAgent.Core/            entities, LLM contracts, abstractions, DTOs
    ShoppingAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    ShoppingAgent.Api/             5 controllers, Program.cs
  frontend/src/
    pages/                        Dashboard, Chat, ShoppingList, History
    components/Layout.tsx         sidebar
    hooks/useVoice.ts             same voice hook as the other apps
```

### How a chat turn flows

Same bounded tool-use loop pattern as the other apps: `POST /api/chat` -> `OrchestratorService`
builds a shopping-assistant system prompt -> loops against `ILlmProvider` with the tool list ->
`ToolRegistry` dispatches to `ITool` implementations -> PostgreSQL. The destructive tool
(`delete_item`) requires `confirmed=true`.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps - separate database)
- An OpenAI API key (can reuse the same key as the other apps)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres shopping_agent      # or: CREATE DATABASE shopping_agent;
cd backend/src/ShoppingAgent.Api
dotnet user-secrets set "ConnectionStrings:ShoppingAgent" "Host=localhost;Port=5432;Database=shopping_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/ShoppingAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5082> - health check at `/api/health`.

To manage migrations manually:

```bash
$env:SHOPPING_AGENT_DB = "Host=localhost;Port=5432;Database=shopping_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/ShoppingAgent.Infrastructure --startup-project backend/src/ShoppingAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5178>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Active item count, purchased-this-week count, estimated total, recent items |
| GET/POST/PUT/DELETE | `/api/items[/{id}]` | Shopping list items (`?purchased=` filters the list) |

## Tools the assistant can call

`add_item` · `get_shopping_list` · `mark_purchased` · `update_item` · `delete_item*` ·
`get_purchase_history` · `current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Budget tracking / spend trends over time (dashboard shows a point-in-time estimated total only)
- Recipe or meal-plan driven list generation
- Store aisle ordering or multi-store route planning
