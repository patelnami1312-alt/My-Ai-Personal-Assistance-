# Fitness Agent

A real chat-driven fitness coach and tracker — a separate app from the Personal Assistant and
Coding Agent (own backend, frontend, port, and database), living as a sibling folder in the
same repo. See the [root README](../README.md) for why it's split out this way.

Log workouts, body weight, and meals by talking to it (text or voice); it also has direct
CRUD pages for each, a dashboard, and goal tracking.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `fitness_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the Personal Assistant |

```
fitness-agent/
  backend/src/
    FitnessAgent.Core/            entities, LLM contracts, abstractions, DTOs
    FitnessAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    FitnessAgent.Api/             6 controllers, Program.cs
  frontend/src/
    pages/                        Dashboard, Chat, Workouts, BodyWeight, Nutrition, Goals
    components/Layout.tsx         sidebar (+ links to the other two apps)
    hooks/useVoice.ts             same voice hook as the Personal Assistant
```

### How a chat turn flows

Same bounded tool-use loop pattern as the Personal Assistant (see its README), minus the
memory-extraction subsystem: `POST /api/chat` → `OrchestratorService` builds a fitness-coach
system prompt → loops against `ILlmProvider` with the tool list → `ToolRegistry` dispatches to
`ITool` implementations → PostgreSQL. Destructive tools (`delete_*`) require `confirmed=true`.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps — separate database)
- An OpenAI API key (can reuse the same key as the Personal Assistant)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres fitness_agent      # or: CREATE DATABASE fitness_agent;
cd backend/src/FitnessAgent.Api
dotnet user-secrets set "ConnectionStrings:FitnessAgent" "Host=localhost;Port=5432;Database=fitness_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/FitnessAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5079> — health check at `/api/health`.

To manage migrations manually:

```bash
$env:FITNESS_AGENT_DB = "Host=localhost;Port=5432;Database=fitness_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/FitnessAgent.Infrastructure --startup-project backend/src/FitnessAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5175> (or click "Fitness Agent ↗" from either other app's sidebar).
`/api` is proxied to the backend (override with `VITE_API_TARGET`). Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Weekly workout count, latest weight, 30-day trend, active goals, recent workouts |
| GET/POST/DELETE | `/api/workouts[/{id}]` | Workout log |
| GET/POST/DELETE | `/api/bodyweight[/{id}]` | Body weight log |
| GET/POST/DELETE | `/api/nutrition[/{id}]` | Meal log |
| GET/POST/PUT/DELETE | `/api/goals[/{id}]` | Goals |

## Tools the coach can call

`log_workout` · `get_workouts` · `delete_workout*` ·
`log_bodyweight` · `get_bodyweight_history` · `delete_bodyweight_entry*` ·
`log_meal` · `get_meals` · `delete_meal*` ·
`set_goal` · `get_goals` · `update_goal` · `delete_goal*` ·
`current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Weight/workout trend charts (dashboard currently shows numbers, not graphs)
- Structured workout programs / templates
- Nutrition targets vs. actuals (macros are logged but not compared against goals)
