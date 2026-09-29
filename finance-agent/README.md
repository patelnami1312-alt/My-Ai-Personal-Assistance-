# Finance Agent

A real chat-driven personal finance assistant and tracker - a separate app from the Personal
Assistant, Coding Agent, and Fitness Agent (own backend, frontend, port, and database), living
as a sibling folder in the same repo. See the [root README](../README.md) for why it's split
out this way.

Log income, expenses, accounts, budgets, and bills by talking to it (text or voice); it also has
direct CRUD pages for each, and a dashboard summarizing the month.

## Stack

| Layer    | Tech |
|----------|------|
| Frontend | React 19 + TypeScript + Vite + React Router, Web Speech API |
| Backend  | ASP.NET Core 9 Web API (controllers) |
| Data     | PostgreSQL + EF Core (Npgsql), own database `finance_agent` |
| LLM      | OpenAI, behind the same `ILlmProvider` pattern as the other apps |

```
finance-agent/
  backend/src/
    FinanceAgent.Core/            entities, LLM contracts, abstractions, DTOs
    FinanceAgent.Infrastructure/  EF Core, OpenAiLlmProvider, Tools/ (framework + domain tools), Orchestration/
    FinanceAgent.Api/             7 controllers, Program.cs
  frontend/src/
    pages/                        Dashboard, Chat, Transactions, Accounts, Budgets, Bills
    components/Layout.tsx         sidebar
    hooks/useVoice.ts             same voice hook as the other apps
```

### How a chat turn flows

Same bounded tool-use loop pattern as the other apps: `POST /api/chat` -> `OrchestratorService`
builds a personal-finance-assistant system prompt -> loops against `ILlmProvider` with the tool
list -> `ToolRegistry` dispatches to `ITool` implementations -> PostgreSQL. Destructive tools
(`delete_*`) require `confirmed=true`.

## Prerequisites

- .NET SDK 9
- Node 20+
- PostgreSQL running locally (can be the same server as the other apps - separate database)
- An OpenAI API key (can reuse the same key as the other apps)

## Setup

### 1. Database + secrets

```bash
createdb -U postgres finance_agent      # or: CREATE DATABASE finance_agent;
cd backend/src/FinanceAgent.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:FinanceAgent" "Host=localhost;Port=5432;Database=finance_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet user-secrets set "Llm:ApiKey" "sk-..."
```

### 2. Run the backend

```bash
cd backend/src/FinanceAgent.Api
dotnet run
```

Migrations apply automatically on startup. API: <http://localhost:5080> - health check at `/api/health`.

To manage migrations manually:

```bash
$env:FINANCE_AGENT_DB = "Host=localhost;Port=5432;Database=finance_agent;Username=postgres;Password=YOUR_PG_PASSWORD"
dotnet ef database update --project backend/src/FinanceAgent.Infrastructure --startup-project backend/src/FinanceAgent.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:5176>. `/api` is proxied to the backend (override with `VITE_API_TARGET`).
Voice needs Chrome or Edge.

## API surface

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/chat` | Send a message, run the orchestrator |
| GET/DELETE | `/api/conversations[/{id}]` | Conversation history |
| GET | `/api/dashboard` | Income/expense/net this month, total balance, upcoming bills, budget status |
| GET/POST/DELETE | `/api/transactions[/{id}]` | Income and expense log |
| GET/POST/PUT/DELETE | `/api/accounts[/{id}]` | Financial accounts |
| GET/POST/DELETE | `/api/budgets[/{id}]` | Category budgets |
| GET/POST/PUT/DELETE | `/api/bills[/{id}]` | Bills |

## Tools the assistant can call

`log_transaction` &middot; `get_transactions` &middot; `delete_transaction*` &middot; `get_spending_summary` &middot;
`add_account` &middot; `get_accounts` &middot; `update_account_balance` &middot;
`set_budget` &middot; `get_budgets` &middot;
`add_bill` &middot; `get_bills` &middot; `mark_bill_paid` &middot; `delete_bill*` &middot;
`current_datetime`

`*` = requires explicit user confirmation before it runs.

## Not built yet

- Spending trend charts (dashboard currently shows numbers and a simple budget bar, not graphs)
- Multi-currency support
- Recurring transaction templates (only bills support recurrence)
- Net worth tracking over time
