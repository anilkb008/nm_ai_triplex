# NM i AI — Tripletex AI Accounting Agent

Solution for the **NM i AI Tripletex challenge**: an AI agent that completes accounting tasks in Tripletex using Claude Opus 4.6 and a .NET MCP Server.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Competition validator                                           │
│       POST /solve  ──────────────────────────────────────┐      │
└──────────────────────────────────────────────────────────┼──────┘
                                                           ▼
┌──────────────────────── Azure VM ────────────────────────────────┐
│                                                                  │
│   ┌────────────────────────────────┐                            │
│   │  SolveApi   :8080              │                            │
│   │  POST /solve                   │                            │
│   │  • Builds system prompt with   │                            │
│   │    Tripletex credentials       │                            │
│   │  • ONE call to Anthropic API   │                            │
│   │    (mcp_servers + prompt)      │                            │
│   │  • Returns {"status":"done"}   │                            │
│   └──────────────┬─────────────────┘                            │
│                  │ POST /v1/messages                            │
│                  ▼ (anthropic-beta: mcp-client-2025-11-20)      │
│   ┌─────────────────────────────────────────┐                  │
│   │  Anthropic API  (claude-opus-4-6)        │                  │
│   │  • Connects to TripletexMcpServer        │                  │
│   │  • Discovers tools automatically         │                  │
│   │  • Calls tools as needed (server-side)   │◄─ tool calls ──┐ │
│   └─────────────────────────────────────────┘                 │ │
│                                                                │ │
│   ┌─────────────────────────────────────────┐                 │ │
│   │  TripletexMcpServer  :5000  (public)     │─── tool results┘ │
│   │  GET  /           (info)                 │                  │
│   │  SSE  /mcp        (MCP protocol)         │                  │
│   │  • 30+ Tripletex tools                   │                  │
│   │  • Calls competition sandbox API         │                  │
│   └──────────────────────────────────────────┘                 │
└──────────────────────────────────────────────────────────────────┘
```

### How it works

1. Competition validator calls `POST /solve` with task prompt + Tripletex credentials
2. **SolveApi** builds a single Anthropic API request:
   - `mcp_servers`: points to the public URL of TripletexMcpServer
   - `system`: includes credentials + agent instructions
   - `messages`: user prompt + any attached files
3. **Anthropic** connects to TripletexMcpServer, discovers all tools, and runs the
   tool-calling loop entirely server-side — no manual loop in our code
4. **TripletexMcpServer** executes tool calls against the competition's Tripletex sandbox
5. When Anthropic is done, SolveApi returns `{"status": "completed"}`

> **Key benefit**: Anthropic's infrastructure manages retries, parallel tool calls, and
> multi-step reasoning. Our code stays simple and stateless.

---

## Projects

| Project | Description |
|---------|-------------|
| `src/SolveApi/` | Competition endpoint — .NET 8 Web API (thin wrapper around Anthropic API) |
| `src/TripletexMcpServer/` | Tripletex MCP Server — .NET 8 with `ModelContextProtocol.AspNetCore` |

---

## Prerequisites

- Docker + Docker Compose
- Anthropic API key
- Azure VM (or any host) with a public IP / domain for HTTPS
- Nginx for TLS termination

---

## Quick Start

### 1. Clone & configure

```bash
git clone <repo>
cd nm_ai_triplex
cp .env.example .env
nano .env  # Fill in ANTHROPIC_API_KEY and MCP_SERVER_URL
```

### 2. Build & start

```bash
docker compose up --build -d
```

Services:
- **SolveApi**: `http://localhost:8080`
- **TripletexMcpServer**: `http://localhost:5000`

### 3. Test locally (requires ngrok or public URL for MCP)

```bash
# Expose MCP server publicly for local testing
ngrok http 5000
# → copy the https URL, e.g. https://abc123.ngrok.io
# Update .env: MCP_SERVER_URL=https://abc123.ngrok.io/mcp
# Restart: docker compose up -d solve-api

curl -X POST http://localhost:8080/solve \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Create an employee named John Doe with email john@example.com",
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

### 1. Set up Nginx with HTTPS

The TripletexMcpServer **must be publicly accessible** — Anthropic's servers call it directly.

```nginx
# /etc/nginx/sites-available/triplex
server {
    listen 443 ssl;
    server_name your-vm.example.com;

    ssl_certificate     /etc/ssl/certs/cert.pem;
    ssl_certificate_key /etc/ssl/private/key.pem;

    # Competition endpoint (submit this URL to the platform)
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_read_timeout 600s;   # 10 min for Anthropic MCP calls
    }

    # MCP server (Anthropic connects here — must be public)
    location /mcp {
        proxy_pass http://localhost:5000/mcp;
        proxy_set_header Host $host;
        proxy_set_header Connection '';
        proxy_http_version 1.1;
        proxy_read_timeout 600s;
        proxy_buffering off;       # Required for SSE
        chunked_transfer_encoding on;
    }
}

server {
    listen 80;
    server_name your-vm.example.com;
    return 301 https://$host$request_uri;
}
```

```bash
sudo ln -s /etc/nginx/sites-available/triplex /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### 2. Configure and deploy

```bash
# .env on the VM
ANTHROPIC_API_KEY=sk-ant-api03-...
MCP_SERVER_URL=https://your-vm.example.com/mcp

docker compose up --build -d
```

### 3. Submit to competition

Submit: `https://your-vm.example.com` (the `/solve` endpoint)

---

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `ANTHROPIC_API_KEY` | Yes | Anthropic API key |
| `MCP_SERVER_URL` | Yes | **Public HTTPS URL** of TripletexMcpServer `/mcp` endpoint. Anthropic's servers must reach this. |

---

## Available Tripletex Tools (on TripletexMcpServer)

Anthropic discovers these automatically via MCP — no manual registration in SolveApi.

| Category | Tools |
|----------|-------|
| **Company** | `get_company_info`, `get_accounts`, `get_currencies`, `get_vat_types`, `get_departments`, `create_department` |
| **Employees** | `search_employees`, `get_employee`, `create_employee`, `update_employee` |
| **Customers** | `search_customers`, `get_customer`, `create_customer`, `update_customer` |
| **Suppliers** | `search_suppliers`, `create_supplier` |
| **Products** | `search_products`, `create_product` |
| **Invoices** | `search_invoices`, `get_invoice`, `create_invoice`, `send_invoice` |
| **Projects** | `search_projects`, `get_project`, `create_project` |
| **Timesheet** | `search_timesheet_entries`, `create_timesheet_entry` |
| **Ledger** | `search_vouchers`, `create_voucher`, `get_vat_types` |
| **Fallback** | `tripletex_generic_get`, `tripletex_generic_post` |

Each tool requires `baseUrl` and `sessionToken` parameters — injected from the system prompt by Claude automatically.

---

## MCP Server — Standalone Use (Claude Desktop)

The TripletexMcpServer also works as a standalone MCP server for Claude Desktop.

Add to `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "tripletex": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

Each tool call requires `baseUrl` and `sessionToken` (your sandbox credentials).

---

## Supported Task Languages

**Norwegian** (Bokmål/Nynorsk), **English**, **Swedish**, **Danish**, **German**, **French**, **Spanish**

---

## Development

```bash
# Run SolveApi locally
cd src/SolveApi
export ANTHROPIC_API_KEY=sk-ant-...
export MCP_SERVER_URL=https://your-public-mcp-url/mcp
dotnet run

# Run MCP Server locally
cd src/TripletexMcpServer
dotnet run
```
