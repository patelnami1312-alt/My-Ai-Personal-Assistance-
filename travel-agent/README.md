# Travel Agent

A real chat-driven trip planner and tracker — a separate app from the Personal Assistant,
Coding Agent, and Fitness Agent (own backend, frontend, port, and database), living as a
sibling folder in the same repo. See the [root README](../README.md) for why it's split out
this way.

Plan trips, build itineraries, and track packing lists by talking to it (text or voice); it
also has direct CRUD pages for trips, itineraries, and packing lists, plus a dashboard.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `travel_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the other apps |

```
travel-agent/
  backend/src/
    TravelAgent.Core/            entities, LLM contracts, abstractions, DTOs
    TravelAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    TravelAgent.Api/             7 controllers, Program.cs
  frontend/src/
    pages/                        Dashboard, Chat, Trips, TripDetail
    components/Layout.tsx         sidebar
    hooks/useVoice.ts             same voice hook as the other apps
```

### How a chat turn flows

Same bounded tool-use loop pattern as the other apps: `POST /api/chat` -> `OrchestratorService`
builds a travel-assistant system prompt -> loops against `ILlmProvider` with the tool list ->
`ToolRegistry` dispatches to `ITool` implementations -> PostgreSQL. Destructive tools
(`delete_*`) require `confirmed=true`.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps - separate database)
- An OpenAI API key (can reuse the same key as the other apps)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres travel_agent      # or: CREATE DATABASE travel_agent;
cd backend/src/TravelAgent.Api
dotnet user-secrets set "ConnectionStrings:TravelAgent" "Host=localhost;Port=5432;Database=travel_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/TravelAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5084> - health check at `/api/health`.

To manage migrations manually:

```bash
$env:TRAVEL_AGENT_DB = "Host=localhost;Port=5432;Database=travel_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5180>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Total trips, upcoming trip, packing progress for the upcoming trip |
| GET/POST/PUT/DELETE | `/api/trips[/{id}]` | Trips |
| GET/POST/DELETE | `/api/itinerary?tripId=` | Itinerary items for a trip |
| GET/POST/PUT/DELETE | `/api/packing?tripId=` | Packing items for a trip (PUT toggles packed) |

## Tools the assistant can call

`create_trip` &middot; `get_trips` &middot; `update_trip` &middot; `delete_trip*` &middot;
`add_itinerary_item` &middot; `get_itinerary` &middot; `delete_itinerary_item*` &middot;
`add_packing_item` &middot; `get_packing_list` &middot; `check_packing_item` &middot; `delete_packing_item*` &middot;
`current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Flight/hotel price lookups or booking integration (all data is entered manually or by the assistant)
- Trip budgeting / expense tracking
- Multi-traveler itineraries (single-user local tool, no sharing or collaboration)
