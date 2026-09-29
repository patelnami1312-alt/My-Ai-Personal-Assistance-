# My Personal AI — monorepo

Independent apps living side by side, per the original vision (orchestrator + separate
agents — Personal, Coding, Fitness, Finance, Home, Shopping, Learning, Travel). Each app has
its own backend, frontend, port, and database. Nothing is shared at runtime; they're just
checked in together.

```
AI-Personal-Assistant/
  backend/   frontend/          ← Personal Assistant app,      :5067 / :5173 (this README)
  coding-agent/    backend/ frontend/   ← Coding Agent,         :5078 / :5174 (coding-agent/README.md)
  fitness-agent/   backend/ frontend/   ← Fitness Agent,        :5079 / :5175 (fitness-agent/README.md)
  finance-agent/   backend/ frontend/   ← Finance Agent,        :5080 / :5176 (finance-agent/README.md)
  home-agent/      backend/ frontend/   ← Home Management Agent,:5081 / :5177 (home-agent/README.md)
  shopping-agent/  backend/ frontend/   ← Shopping Agent,       :5082 / :5178 (shopping-agent/README.md)
  learning-agent/  backend/ frontend/   ← Learning Agent,       :5083 / :5179 (learning-agent/README.md)
  travel-agent/    backend/ frontend/   ← Travel Agent,         :5084 / :5180 (travel-agent/README.md)
```

Every app's sidebar (or topbar) links out to the others — they open in a new tab since
they're genuinely separate applications, not routes within one SPA.

Why separate apps instead of one shared backend: each agent gets its own database, its own
deployable, and its own clean room — no risk of one agent's bugs or schema changes touching
the other. The cost is some duplicated plumbing (EF setup, CORS, JSON config), which is small
and worth it for the isolation.

---

# Personal Assistant

Voice + text in, an LLM orchestrator that reasons and calls real tools (tasks, calendar,
notes, long-term memory), confirmation gates on destructive actions, automatic memory
extraction, and a proactive daily briefing.

This build covers **Steps 1–10** of the roadmap: foundation, dashboard UI, voice I/O,
the tool system, tasks, calendar, memory (+ intelligent extraction), and notes.

## Stack

| Layer     | Tech                                                    |
|-----------|--------------------------------------------------------|
| Frontend  | React 19 + TypeScript + Vite, React Router, Web Speech API |
| Backend   | ASP.NET Core 9 Web API (controllers)                   |
| Data      | PostgreSQL + EF Core (Npgsql)                          |
| LLM       | OpenAI **or** Claude, behind `ILlmProvider` (switch with `Llm:Provider`) |

```
backend/src/
  Assistant.Core/                entities, DTOs, abstractions (ILlmProvider, ITool, IOrchestrator…)
  Assistant.Infrastructure/
    Data/ Llm/ Orchestration/ Tools/   shared plumbing (DbContext, LLM providers, agent loop, tool framework)
    Agents/Personal/                    tasks, calendar, notes, memory, briefing
  Assistant.Api/                 controllers, DI wiring, startup migration
frontend/src/
  components/ hooks/ api.ts types.ts    shell + shared
  agents/personal/                       Home, Assistant, Tasks, Calendar, Notes, Memory, Settings
```

The sidebar has a "Coding Agent ↗" link that opens the separate Coding Agent app
(http://localhost:5174) in a new tab — it's a different application, not a route in this one.

### How a turn flows

```
UI (voice/text) → POST /api/chat
    → OrchestratorService
        → builds system prompt (profile + preferences + memories + current time)
        → bounded tool-use loop against ILlmProvider
            → ToolRegistry dispatches to ITool implementations → PostgreSQL
            → destructive tools return "CONFIRMATION REQUIRED" until confirmed=true
        → persists the exchange, fires background MemoryExtractionService
    → reply returned, spoken via speechSynthesis
```

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (or anywhere reachable)
- An LLM API key — OpenAI (<https://platform.openai.com/api-keys>) or Anthropic (<https://console.anthropic.com>)

## Setup

### 1. Database + secrets

Create the database:

```bash
createdb -U postgres ai_personal_assistant     # or: CREATE DATABASE ai_personal_assistant;
```

Set the real values (stored outside the repo in .NET user-secrets):

```bash
cd backend/src/Assistant.Api
dotnet user-secrets set "ConnectionStrings:Assistant" "Host=localhost;Port=5432;Database=ai_personal_assistant;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."          # OpenAI key (default), or an sk-ant-... Claude key
```

Pick the provider in `appsettings.json` → `Llm:Provider` (`"openai"` or `"anthropic"`).
The key also falls back to the `OPENAI_API_KEY` / `ANTHROPIC_API_KEY` environment variable.

### 2. Run the backend

```bash
cd backend/src/Assistant.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5067> — health check at `/api/health`.

To manage migrations manually:

```bash
# from repo root
$env:ASSISTANT_DB = "Host=localhost;Port=5432;Database=ai_personal_assistant;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/Assistant.Infrastructure --startup-project backend/src/Assistant.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5173>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice input needs Chrome or Edge; text works in any browser.

## Configuration (`appsettings.json` → `Llm`)

| Key            | Default    | Notes                                                             |
|----------------|------------|------------------------------------------------------------------|
| `Llm:Provider` | `openai`   | `openai` or `anthropic` — selects the `ILlmProvider` implementation. |
| `Llm:Model`    | `gpt-4o`   | openai: `gpt-4o` / `gpt-4o-mini`. anthropic: `claude-opus-5` / `claude-sonnet-5`. |
| `Llm:MaxTokens`| `1500`     | Per assistant turn.                                               |
| `Llm:ApiKey`   | *(user-secrets)* | Falls back to `OPENAI_API_KEY` / `ANTHROPIC_API_KEY`.       |

Switching providers is one config line — no code change. Both implementations live in
`Assistant.Infrastructure/Llm/` and satisfy the same `ILlmProvider` interface.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/briefing` | Proactive daily briefing |
| CRUD | `/api/tasks`, `/api/notes`, `/api/events`, `/api/memories` | Direct data access for the dashboard |
| GET/PUT | `/api/profile` | User profile |

## Tools the assistant can call

`create_task` · `get_tasks` · `update_task` · `complete_task` · `delete_task*` ·
`create_note` · `search_notes` · `delete_note*` ·
`create_event` · `get_events` · `update_event` · `delete_event*` · `find_free_time` ·
`recall` · `forget*` · `update_profile` ·
`current_datetime` · `calculator` · `web_search` (stub — Step 11)

`*` = requires explicit user confirmation before it runs. (There's no `remember` tool —
long-term memory is written by the background `MemoryExtractionService` after each turn,
which avoids the model and the extractor both storing the same fact twice.)

## Roadmap beyond this build

- **11** Web search — swap the `WebSearchTool` stub for a real provider
- **12** File understanding (PDF/DOCX) + RAG with pgvector (also upgrades memory retrieval from keyword to embeddings)
- **13** Email assistant (read/summarize/draft, send behind confirmation)
- **14–15** Scheduled proactive briefings (push/notification)
- **16** Multi-agent orchestrator — `coding-agent/`, `fitness-agent/`, `finance-agent/`, `home-agent/`, `shopping-agent/`, `learning-agent/`, and `travel-agent/` are the sibling agents built so far; more follow the same pattern (own top-level folder, own backend+frontend+DB+ports)
- Auth + multi-user (every row is already `UserId`-scoped; `AssistantDbContext.DefaultUserId` is the single seam to replace)

---

# Coding Agent

See **[coding-agent/README.md](coding-agent/README.md)**.

---

# Fitness Agent

See **[fitness-agent/README.md](fitness-agent/README.md)**.

---

# Finance Agent

Chat-driven personal finance tracker — transactions, accounts, budgets, and bills.
See **[finance-agent/README.md](finance-agent/README.md)**.

---

# Home Management Agent

Chores (with recurrence), home maintenance schedule, and household inventory tracking.
See **[home-agent/README.md](home-agent/README.md)**.

---

# Shopping Agent

Chat-driven shopping list with purchase history and estimated-cost tracking.
See **[shopping-agent/README.md](shopping-agent/README.md)**.

---

# Learning Agent

Course/study tracker — study sessions, notes, and learning goals.
See **[learning-agent/README.md](learning-agent/README.md)**.

---

# Travel Agent

Trip planner — itineraries and packing lists per trip.
See **[travel-agent/README.md](travel-agent/README.md)**.
