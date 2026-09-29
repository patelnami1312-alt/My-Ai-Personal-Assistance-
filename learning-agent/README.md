# Learning Agent

A real chat-driven learning coach and tracker — a separate app from the Personal Assistant,
Coding Agent, and Fitness Agent (own backend, frontend, port, and database), living as a
sibling folder in the same repo. See the [root README](../README.md) for why it's split out
this way.

Track courses/subjects you're studying, log study sessions, save notes, and set learning
goals by talking to it (text or voice); it also has direct CRUD pages for each, and a
dashboard.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `learning_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the other apps |

```
learning-agent/
  backend/src/
    LearningAgent.Core/            entities, LLM contracts, abstractions, DTOs
    LearningAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    LearningAgent.Api/             8 controllers, Program.cs
  frontend/src/
    pages/                         Dashboard, Chat, Courses, StudyLog, Notes, Goals
    components/Layout.tsx          sidebar
    hooks/useVoice.ts              same voice hook as the other apps
```

### How a chat turn flows

Same bounded tool-use loop pattern as the other agents: `POST /api/chat` -> `OrchestratorService`
builds a learning-coach system prompt -> loops against `ILlmProvider` with the tool list ->
`ToolRegistry` dispatches to `ITool` implementations -> PostgreSQL. Destructive tools
(`delete_note`, `delete_learning_goal`) require `confirmed=true`.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps - separate database)
- An OpenAI API key (can reuse the same key as the other apps)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres learning_agent      # or: CREATE DATABASE learning_agent;
cd backend/src/LearningAgent.Api
dotnet user-secrets set "ConnectionStrings:LearningAgent" "Host=localhost;Port=5432;Database=learning_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/LearningAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5083> - health check at `/api/health`.

To manage migrations manually:

```bash
$env:LEARNING_AGENT_DB = "Host=localhost;Port=5432;Database=learning_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/LearningAgent.Infrastructure --startup-project backend/src/LearningAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5179>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Weekly study minutes, active course/goal counts, recent sessions |
| GET/POST/PUT/DELETE | `/api/courses[/{id}]` | Courses |
| GET/POST/DELETE | `/api/study-sessions[/{id}]` | Study session log |
| GET/POST/PUT/DELETE | `/api/notes[/{id}]` | Notes (`?q=` search on title/content) |
| GET/POST/PUT/DELETE | `/api/goals[/{id}]` | Learning goals |

## Tools the assistant can call

`add_course` &middot; `get_courses` &middot; `update_course_status` &middot;
`log_study_session` &middot; `get_study_sessions` &middot; `get_study_summary` &middot;
`add_note` &middot; `search_notes` &middot; `delete_note*` &middot;
`set_learning_goal` &middot; `get_learning_goals` &middot; `update_learning_goal` &middot; `delete_learning_goal*` &middot;
`current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Study time trend charts (dashboard currently shows numbers, not graphs)
- Spaced-repetition / flashcard review
- Per-course progress tracking against goals
