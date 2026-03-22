# NM i AI — Tripletex AI Accounting Agent

Solution for the **NM i AI Tripletex challenge**: an AI agent that completes accounting tasks in Tripletex using the Claude API and a purpose-built Tripletex MCP Server.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                       Azure VM / Docker                      │
│                                                             │
│   ┌──────────────────────────────────────┐                  │
│   │           SolveApi  :8080            │  ← competition   │
│   │  POST /solve                         │    calls this     │
│   │  • Receives task prompt + credentials│                  │
│   │  • Runs Claude agentic loop          │                  │
│   │  • Executes Tripletex API calls      │                  │
│   │  • Returns {"status": "completed"}   │                  │
│   └──────────────────────────────────────┘                  │
│                                                             │
│   ┌──────────────────────────────────────┐                  │
│   │      TripletexMcpServer  :5000        │  ← optional MCP  │
│   │  GET  /                (info)         │    for Claude    │
│   │  SSE  /mcp             (MCP tools)    │    Desktop       │
│   └──────────────────────────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

### How the agent works

1. Competition validator calls `POST /solve` with a task prompt (in one of 7 languages) and Tripletex credentials
2. **SolveApi** extracts credentials and prompt, then starts an **agentic loop** with Claude Sonnet
3. Claude calls **Tripletex tools** (create_employee, create_invoice, etc.) to complete the task
4. Tool calls are executed directly against the competition's Tripletex sandbox via the provided `base_url` + `session_token`
5. When Claude finishes, `{"status": "completed"}` is returned

---

## Projects

| Project | Description |
|---------|-------------|
| `src/SolveApi/` | Main competition endpoint — `.NET 8` Web API |
| `src/TripletexMcpServer/` | Standalone MCP Server — `.NET 8` with `ModelContextProtocol.AspNetCore` |

---

## Quick Start

### Prerequisites
- Docker + Docker Compose
- Anthropic API key

### 1. Clone & configure

```bash
git clone <repo>
cd nm_ai_triplex
cp .env.example .env
# Edit .env and set ANTHROPIC_API_KEY=sk-ant-api03-...
```

### 2. Build & run

```bash
docker compose up --build -d
```

Services:
- **SolveApi**: `http://localhost:8080`
- **MCP Server**: `http://localhost:5000`

### 3. Test locally

```bash
curl -X POST http://localhost:8080/solve \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Create an employee named John Doe with email john.doe@example.com",
    "files": [],
    "tripletex_credentials": {
      "base_url": "https://your-sandbox.tripletex.dev/v2",
      "session_token": "your-session-token"
    }
  }'
```

Expected response:
```json
{"status": "completed"}
```

---

## Deploy to Azure VM

### Option A: Docker Compose (recommended)

```bash
# On your Azure VM:
git clone <repo> && cd nm_ai_triplex
cp .env.example .env && nano .env  # set ANTHROPIC_API_KEY

docker compose up --build -d

# Expose port 8080 via Nginx or Azure Load Balancer for HTTPS
```

### Option B: Individual containers

```bash
# Build
docker build -t triplex-solve-api ./src/SolveApi
docker build -t triplex-mcp-server ./src/TripletexMcpServer

# Run SolveApi
docker run -d \
  --name triplex-solve-api \
  -p 8080:8080 \
  -e ANTHROPIC_API_KEY=sk-ant-... \
  triplex-solve-api

# Run MCP Server (optional)
docker run -d \
  --name triplex-mcp-server \
  -p 5000:5000 \
  triplex-mcp-server
```

### HTTPS with Nginx (required for competition)

```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate     /etc/ssl/certs/your-cert.pem;
    ssl_certificate_key /etc/ssl/private/your-key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_read_timeout 300s;  # 5 min for long agent runs
    }
}
```

---

## Available Tools (Claude's Tripletex toolkit)

| Category | Tools |
|----------|-------|
| **Company** | `get_company_info`, `get_accounts`, `get_currencies`, `get_vat_types`, `get_departments`, `create_department` |
| **Employees** | `search_employees`, `get_employee`, `create_employee`, `update_employee` |
| **Customers** | `search_customers`, `get_customer`, `create_customer`, `update_customer` |
| **Suppliers** | `search_suppliers`, `get_supplier`, `create_supplier`, `update_supplier` |
| **Products** | `search_products`, `get_product`, `create_product`, `update_product` |
| **Invoices** | `search_invoices`, `get_invoice`, `create_invoice`, `send_invoice` |
| **Orders** | `search_orders`, `create_order` |
| **Projects** | `search_projects`, `get_project`, `create_project`, `update_project` |
| **Timesheet** | `search_timesheet_entries`, `create_timesheet_entry`, `update_timesheet_entry` |
| **Ledger** | `search_vouchers`, `get_voucher`, `create_voucher`, `search_postings` |
| **Fallback** | `tripletex_get`, `tripletex_post` |

---

## MCP Server (for Claude Desktop)

The `TripletexMcpServer` is a standalone [Model Context Protocol](https://modelcontextprotocol.io) server you can connect from Claude Desktop to explore Tripletex interactively.

Add to your Claude Desktop config (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "tripletex": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

Each tool requires `baseUrl` and `sessionToken` parameters (your sandbox credentials).

---

## Configuration

| Env Variable | Required | Description |
|-------------|----------|-------------|
| `ANTHROPIC_API_KEY` | Yes | Your Anthropic API key |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` (default) |
| `ASPNETCORE_URLS` | No | `http://+:8080` (default) |

---

## Supported Task Languages

The agent handles prompts in: **Norwegian** (Bokmål/Nynorsk), **English**, **Swedish**, **Danish**, **German**, **French**, **Spanish**.

---

## Development

```bash
# Run SolveApi locally
cd src/SolveApi
export ANTHROPIC_API_KEY=sk-ant-...
dotnet run

# Run MCP Server locally
cd src/TripletexMcpServer
dotnet run
```
