# 🛒 FoundryCommerceDemo

> **Enterprise E-Commerce Operations Center — Multi-Agent Demo**
> Built with Microsoft Agent Framework v1.0 + Microsoft Foundry.
> A simple end-to-end pipeline:
> fraud check → inventory → fulfillment → customer email.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Agent Framework](https://img.shields.io/badge/Agent%20Framework-v1.3-0078D4)](https://github.com/microsoft/agent-framework)
[![Foundry](https://img.shields.io/badge/Microsoft-Foundry-2560E0)](https://foundry.azure.com)

---

## 📖 Table of Contents

1. [What is this?](#-what-is-this)
2. [Architecture](#-architecture)
3. [Hosting model — this is a LOCAL agent demo](#-hosting-model--this-is-a-local-agent-demo)
4. [Other hosting options](#-other-hosting-options)
5. [What you need](#-what-you-need)
6. [Quick start](#-quick-start)
7. [Azure setup (`az login` step by step)](#-azure-setup-az-login-step-by-step)
8. [Environment variables](#-environment-variables)
9. [How to run](#-how-to-run)
10. [Project structure](#-project-structure)
11. [Demo orders](#-demo-orders)
12. [Local mode (Ollama)](#-local-mode-ollama)
13. [Troubleshooting](#-troubleshooting)
14. [License](#-license)

---

## 🎯 What is this?

This demo shows an online shop. When a new order comes in, **four AI agents**
work together like a team:

| Agent | What it does |
|-------|--------------|
| 🛡️ **FraudDetectionAgent** | Looks for risky orders. Checks the country, the device, and how many orders the customer made today. |
| 📦 **InventoryAgent** | Checks if the products are in stock. Looks at all warehouses. Can reserve items. |
| 🚚 **FulfillmentAgent** | Picks the best warehouse. Calculates shipping cost. Can split the order if needed. |
| 💬 **CustomerAgent** | Writes a nice email to the customer. Never shares secret data (like fraud scores). |

### Why is this cool?

- ✅ **Real C# code** does the math. The AI does **not** invent numbers.
- ✅ **Multiple warehouses** with real stock data.
- ✅ **Split shipments** when one warehouse is not enough.
- ✅ **Agent-as-a-tool**: one agent can use another agent like a function.
- ✅ **Local mode**: payment data stays on your computer (good for PCI-DSS).

---

## 🏠 Hosting model — this is a **LOCAL agent demo**

> **Important:** In this repo the agents run **on your computer**, not in the cloud.
> Microsoft Foundry is used **only as the LLM gateway** (model + auth + project scope).
> All agent logic, tools, and state live inside your local `dotnet run` process.

### 📐 Architecture diagrams

| Scenario | SVG (preview) | draw.io (editable) |
|----------|---------------|--------------------|
| 🏠 **Local agents** (this repo) | _inline below_ | — |
| ☁️ **Option 1 — Container Apps** | [option2-container-apps.svg](docs/architecture/option2-container-apps.svg) | [.drawio](docs/architecture/option2-container-apps.drawio) |
| 🏗️ **Option 2 — Foundry Agent Service (Azure resources)** ⭐ | [option3-foundry-agent-service-azure.svg](docs/architecture/option3-foundry-agent-service-azure.svg) | [.drawio](docs/architecture/option3-foundry-agent-service-azure.drawio) |
| 📖 Option 2 — Foundry Agent Service (run lifecycle, story-style) | [option3-foundry-agent-service-detailed.svg](docs/architecture/option3-foundry-agent-service-detailed.svg) | [.drawio](docs/architecture/option3-foundry-agent-service-detailed.drawio) |
| 🤖 Option 2 — Foundry Agent Service (compact overview) | [option3-foundry-agent-service.svg](docs/architecture/option3-foundry-agent-service.svg) | [.drawio](docs/architecture/option3-foundry-agent-service.drawio) |
| ⏱️ **Option 4 — Azure Durable Functions** (long-running workflows) | [option4-durable-functions-azure.svg](docs/architecture/option4-durable-functions-azure.svg) | [.drawio](docs/architecture/option4-durable-functions-azure.drawio) |

### What runs where?

```
┌─────────────────────────────────────────────────┐
│ YOUR MACHINE  (dotnet run)                      │
│                                                 │
│  ┌──────────────────────────────────────────┐   │
│  │ Pipeline orchestrator (Program.cs)       │   │
│  │  ├─ FraudDetectionAgent                  │   │
│  │  ├─ InventoryAgent                       │   │
│  │  ├─ FulfillmentAgent                     │   │
│  │  └─ CustomerAgent                        │   │
│  │                                          │   │
│  │ Tools = your C# code (FraudTools.cs etc.)│   │
│  │ State = in-memory (OrderDatabase)        │   │
│  └──────────────────┬───────────────────────┘   │
└─────────────────────┼───────────────────────────┘
                      │ HTTPS
                      ▼
          ┌────────────────────────┐
          │ Microsoft Foundry      │
          │  • gpt-4o-mini model   │  ← only LLM calls go here
          │  • DefaultAzureCredential
          └────────────────────────┘
```

### What Foundry does (in this demo)

| # | Feature | Used? | Notes |
|---|---------|:-----:|-------|
| 🧠 Model hosting | ✅ | `gpt-4o-mini` |
| 🔐 Authentication | ✅ | `DefaultAzureCredential` + `az login` (no API keys) |
| 📦 Project scope | ✅ | One project for billing, quota, monitoring |
| 🤖 Foundry Agent Service (cloud-hosted agents) | ❌ | Agents are local |
| 💬 Threads / conversation history | ❌ | Not persisted |
| 📚 Vector stores / file search | ❌ | Not used |
| 🔒 Content safety filters | ❌ | Not configured |
| 📊 Tracing / evaluation | ❌ | Add OpenTelemetry to enable |

### Why this design?

- 🚀 **Fast** to demo — no deployment needed.
- 💰 **Cheap** — you only pay for LLM tokens.
- 🧪 **Easy to debug** — set breakpoints in Visual Studio.
- 🎤 **Great for talks** — everything visible on stage.

### Limitations of running locally

- ❌ No state persistence — process restart wipes everything.
- ❌ Single user only.
- ❌ No autoscale.
- ❌ Not production-ready as-is.

👉 If you need any of those, see the next section.

---

## ☁️ Other hosting options

Foundry can host much more than just the model. Here are **three ways** to move
the agents to the cloud, from least to most cloud-native.

### Comparison at a glance

| Option | Code change | State persistence | Multi-user | Scale-to-zero | Best for |
|--------|:-----------:|:-----------------:|:----------:|:-------------:|----------|
| 🏠 **Local** (this repo) | — | ❌ | ❌ | — | Demos, dev loop |
| ☁️ **Container Apps** | Small | ❌ (add Cosmos DB) | ✅ | ✅ | Most teams |
| 🤖 **Foundry Agent Service** | Medium | ✅ Auto | ✅ | ✅ | Cloud-first agents |
| ⏱️ **Durable Functions** | Medium | ✅ Auto | ✅ | ✅ | Long-running workflows |

---

### Option 1 — Azure Container Apps (recommended next step)

Wrap the same .NET 10 code in an ASP.NET Core API and deploy to a serverless
container runtime. Your agent code does **not** change — only `Program.cs`
becomes a web app.

📐 **Architecture diagram:**
[`docs/architecture/option2-container-apps.svg`](docs/architecture/option2-container-apps.svg)
· editable [`.drawio`](docs/architecture/option2-container-apps.drawio)

**Topology:**

```
Client → Front Door / APIM → Container Apps (foundry-commerce-api)
                                   ├─ pulls image from ACR
                                   ├─ uses Managed Identity → Foundry (LLM)
                                   ├─ logs to App Insights
                                   └─ stores data in Cosmos DB / Service Bus
```

**Sketch of the code:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
    new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential()));

var app = builder.Build();

app.MapPost("/api/orders/{orderId}/process", async (string orderId, AIProjectClient client) =>
{
    var fraudAgent = client.AsAIAgent(
        model: "gpt-4o-mini",
        instructions: "...",
        tools: [...]);

    var result = await fraudAgent.RunAsync($"Process order {orderId}");
    return Results.Ok(result);
});

app.Run();
```

**Deploy:**

```powershell
az containerapp up `
  --name foundry-commerce-api `
  --resource-group rg-demo `
  --location swedencentral `
  --source .
```

or with Azure Developer CLI:

```powershell
azd up
```

**Pros**
- ✅ Minimal code changes.
- ✅ Scale-to-zero (you pay $0 when idle).
- ✅ Managed Identity → no secrets in code.
- ✅ HTTPS endpoint, custom domains, revisions, blue/green deploys.

**Cons**
- ❌ You still manage state yourself (add Cosmos DB or SQL).
- ❌ Threads/conversations are not built-in.

---

### Option 2 — Microsoft Foundry **Agent Service** (cloud-hosted agents)

Let Foundry host the agent itself. Tool definitions, instructions, and
conversation threads all live in the cloud. Your code calls the agent and
responds to tool-call requests.

🏗️ **Azure architecture (production-ready resource topology):**
[`docs/architecture/option3-foundry-agent-service-azure.svg`](docs/architecture/option3-foundry-agent-service-azure.svg)
· editable [`.drawio`](docs/architecture/option3-foundry-agent-service-azure.drawio)

This is the **“what do I deploy?”** view. It shows real Azure resources, resource
groups, networking, identity, and data services laid out the way they would look
in production.

**Resource groups in the diagram:**

| RG | Color | Contents |
|----|-------|----------|
| `rg-foundrycommerce-app` | 🟩 Green | Container Apps (api + worker), ACR, UAMI, Key Vault, App Configuration, Service Bus |
| `rg-foundrycommerce-foundry` | 🟧 Orange | Foundry Hub + Project, PersistentAgents, Threads/Runs, model deployments, vector store, content safety |
| `rg-foundrycommerce-data` | 🟪 Purple | Cosmos DB (NoSQL), Azure SQL, Storage Account, Azure Managed Redis |

**Cross-cutting concerns (shown on the side):**

- 🆔 **Microsoft Entra ID** → issues tokens to a User-Assigned Managed Identity. **No API keys anywhere.**
- 🔐 **VNet** (`vnet-foundry-prod`) with private endpoints for Key Vault, ACR, Cosmos DB, Storage, Foundry.
- 🛡️ **Edge** — Front Door + WAF + API Management + DDoS Protection.
- 📊 **Observability** — Application Insights, Log Analytics, Azure Monitor, Foundry built-in tracing, Defender for Cloud.

**Run lifecycle (numbered arrows on the diagram):**

| # | Color | What happens |
|---|-------|--------------|
| ① | 🟧 Orange | Container App calls `Runs.CreateRunAsync` on Foundry Agent Service |
| ② | 🟪 Purple | Foundry replies with `requires_action` → “call tool X” |
| ③ | 🟩 Green | Container App runs the C# tool and posts back via `SubmitToolOutputsAsync` |

For a step-by-step **run lifecycle** explanation (story-style), see
[`option3-foundry-agent-service-detailed.svg`](docs/architecture/option3-foundry-agent-service-detailed.svg).
For a single-page **compact overview**, see
[`option3-foundry-agent-service.svg`](docs/architecture/option3-foundry-agent-service.svg).

**Topology:**

```
Client → Container Apps (thin API)
              │   ① start run
              ▼
       ┌─────────────────────────────────────────┐
       │ Microsoft Foundry — Agent Service       │
       │  • PersistentAgents (definitions)       │
       │  • Threads + Runs (auto-saved state)    │
       │  • Vector store (optional, RAG)         │
       │  • Model deployment (gpt-4o-mini)       │
       │  • Built-in safety + tracing            │
       └──────────────┬──────────────────────────┘
                      │ ② "call tool X"
                      ▼
       Container Apps tool handler → runs C# in your code
                      │ ③ returns tool result
                      ▼
       Foundry continues the run → ④ final reply
```

**Run lifecycle (4 steps):**

| # | Who | What happens |
|---|-----|--------------|
| ① | Your API | `Runs.CreateRunAsync(thread, agent)` |
| ② | Foundry | Sends a `requires_action` event with tool name + args |
| ③ | Your API | Runs the C# tool, posts result back via `SubmitToolOutputsAsync` |
| ④ | Foundry | Continues reasoning, returns the final assistant message |

> 💡 Foundry stores **conversation data** (threads, runs, tool calls).
> Your backend stores **business data** (orders, warehouses) in Cosmos DB / SQL.

**Sketch:**

```csharp
using Azure.AI.Agents.Persistent;

var agentsClient = new PersistentAgentsClient(endpoint, credential);

// 1. Create the agent in Foundry (once)
var agent = await agentsClient.Administration.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: "FraudDetectionAgent",
    instructions: "You analyze fraud risk...",
    tools: [
        new FunctionToolDefinition(
            name: "AnalyzeGeoConsistency",
            description: "Checks geographic consistency",
            parameters: BinaryData.FromString("""
                { "type": "object", "properties": {
                    "orderId": { "type": "string" }
                }}
                """))
    ]);

// 2. Create a conversation thread
var thread = await agentsClient.Threads.CreateThreadAsync();

// 3. Send a message and run
await agentsClient.Messages.CreateMessageAsync(
    thread.Id, MessageRole.User, "Check ORD-50002");
var run = await agentsClient.Runs.CreateRunAsync(thread.Id, agent.Id);
```

**What Foundry stores for you**
- ✅ Agent definition (tools, instructions, model)
- ✅ Thread history (every conversation)
- ✅ Run state (resumable if your code crashes)
- ✅ Tool call audit trail

**Pros**
- ✅ Automatic state, threads, history.
- ✅ Built-in safety, evaluation, tracing in Foundry portal.
- ✅ You only host the **tool execution** (a webhook or function).

**Cons**
- ❌ More refactoring — moves agent definition out of C#.
- ❌ Slightly higher cost (storage + agent runtime).

---

### Option 3 — Azure Durable Functions (long-running workflows)

Use this if your pipeline can take **hours or days** (e.g. waits for a manager
approval, retries failed shipments, cross-day reconciliation).

**Sketch:**

```csharp
[Function(nameof(OrderPipeline))]
public async Task<string> OrderPipeline(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    var orderId = context.GetInput<string>();

    // Step 1: fraud check
    var verdict = await context.CallActivityAsync<string>(
        nameof(RunFraudAgent), orderId);

    if (verdict == "BLOCKED")
        return await context.CallActivityAsync<string>(
            nameof(SendBlockEmail), orderId);

    // Step 2: wait up to 24h for human approval
    var approved = await context.WaitForExternalEvent<bool>(
        "ManagerApproval", TimeSpan.FromHours(24));

    // Step 3: fulfillment
    return await context.CallActivityAsync<string>(
        nameof(RunFulfillmentAgent), orderId);
}
```

**Pros**
- ✅ State and progress are checkpointed automatically.
- ✅ Survives restarts, scale-out, and long waits.
- ✅ Perfect for human-in-the-loop steps.

**Cons**
- ❌ More moving parts (Functions runtime, storage account).
- ❌ Steeper learning curve than Container Apps.

---

### Which option should you pick?

```
             ┌────────────────────────────┐
             │ Do agents need to keep      │
             │ state across requests?      │
             └──────┬─────────────┬───────┘
                 No │             │ Yes
                    ▼             ▼
           ┌─────────────┐   ┌──────────────────────┐
           │ Container   │   │ Pipeline can take    │
           │ Apps        │   │ hours / human waits? │
           │ (Option 1)  │   └────┬─────────┬───────┘
           └─────────────┘     No │         │ Yes
                                  ▼         ▼
                       ┌────────────────┐  ┌────────────────┐
                       │ Foundry Agent  │  │ Durable        │
                       │ Service        │  │ Functions      │
                       │ (Option 2)     │  │ (Option 3)     │
                       └────────────────┘  └────────────────┘
```

**Rule of thumb for this demo:** start with Container Apps. It is the smallest
jump from the local code in this repo and covers ~80% of real-world needs.

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                          NEW ORDER                                │
└──────────────────────────────┬───────────────────────────────────┘
                               ▼
                   ┌───────────────────────┐
                   │ FraudDetectionAgent   │
                   │  • Geo check          │
                   │  • Velocity check     │
                   │  • Final verdict      │
                   └─────────┬─────────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼ BLOCKED                     ▼ APPROVED
   ┌───────────────────┐         ┌────────────────────────┐
   │  CustomerAgent    │         │  FulfillmentAgent      │
   │  writes a polite  │         │   uses ↓ as a tool     │
   │  "we need more    │         │  ┌──────────────────┐  │
   │  info" email.     │         │  │ InventoryAgent   │  │
   │  No secret data.  │         │  │  • CheckStock    │  │
   └───────────────────┘         │  │  • Optimize      │  │
                                 │  │  • Reserve       │  │
                                 │  └──────────────────┘  │
                                 └──────────┬─────────────┘
                                            ▼
                                  ┌───────────────────────┐
                                  │  CustomerAgent        │
                                  │  writes a thank-you   │
                                  │  email with the ETA.  │
                                  └───────────────────────┘
```

---

## 🧰 What you need

| Tool | Version | Note |
|------|---------|------|
| .NET SDK | **10.0** (8.0+ also works) | Check with `dotnet --version` |
| Azure CLI | latest | Check with `az --version` |
| Visual Studio 2022/2026 or VS Code | — | C# Dev Kit is helpful |
| Microsoft Foundry project | — | One model must be deployed (e.g. `gpt-4o-mini`) |
| (Optional) Ollama | latest | For local mode: `llama3.1`, `mistral`, or `qwen2.5` |

> ⚠️ Some models (like `llama3`) **cannot call functions**.
> Please use `llama3.1` or newer.

---

## 🚀 Quick start

```powershell
# 1. Clone the repo
git clone <repo-url>
cd FoundryCommerceDemo

# 2. Sign in to Azure
az login

# 3. Set the Foundry endpoint
$env:FOUNDRY_PROJECT_ENDPOINT = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
$env:FOUNDRY_MODEL_NAME       = "gpt-4o-mini"

# 4. Build and run
dotnet restore
dotnet build
dotnet run
```

---

## 🔐 Azure setup (`az login` step by step)

### 1. Install Azure CLI

```powershell
# Windows (winget)
winget install -e --id Microsoft.AzureCLI

# macOS (brew)
brew update && brew install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

Check that it works:

```powershell
az --version
```

### 2. Sign in to Azure

```powershell
az login
```

A browser window opens. Sign in with your Microsoft account.
Then go back to the terminal.

If you have more than one subscription, list all of them:

```powershell
az account list --output table
```

Pick the subscription you want to use:

```powershell
az account set --subscription "<subscription-id-or-name>"
```

Check the active subscription:

```powershell
az account show
```

### 3. Create a Microsoft Foundry project (if you don't have one)

> Foundry Portal: <https://foundry.azure.com>

1. Click **+ New project** → choose a name, region, and subscription.
2. Open **Models** and deploy a model:
   - 🟢 Fast and cheap: `gpt-4o-mini` or `gpt-4.1-mini`
   - 🔵 More powerful: `gpt-4o`
3. On the project page, copy the **Project endpoint**. It looks like this:

   ```
   https://<resource-name>.services.ai.azure.com/api/projects/<project-name>
   ```

### 4. Give yourself the right role

Your user (or managed identity) needs the **Azure AI User** role on the project:

```powershell
$user      = az ad signed-in-user show --query id -o tsv
$projectId = "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.CognitiveServices/accounts/<resource>"

az role assignment create `
  --assignee $user `
  --role "Azure AI User" `
  --scope $projectId
```

### 5. Test the connection

```powershell
$token = az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
curl -H "Authorization: Bearer $token" $env:FOUNDRY_PROJECT_ENDPOINT
```

If you see `200 OK`, you are ready! ✅

---

## 🔧 Environment variables

| Variable | What it is | Example |
|----------|------------|---------|
| `FOUNDRY_PROJECT_ENDPOINT` | The URL of your Foundry project | `https://admin-5418-resource.services.ai.azure.com/api/projects/admin-5418` |
| `FOUNDRY_MODEL_NAME` | The name of your deployed model | `gpt-4o-mini` |

### PowerShell (only this session)

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
$env:FOUNDRY_MODEL_NAME       = "gpt-4o-mini"
```

### PowerShell (save for later)

```powershell
[Environment]::SetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT", "https://...", "User")
[Environment]::SetEnvironmentVariable("FOUNDRY_MODEL_NAME", "gpt-4o-mini", "User")
```

### Bash / Zsh

```bash
export FOUNDRY_PROJECT_ENDPOINT="https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
export FOUNDRY_MODEL_NAME="gpt-4o-mini"
```

> 💡 **Tip:** In Visual Studio you can also add these in `launchSettings.json`
> under `environmentVariables`.

---

## ▶️ How to run

```powershell
dotnet run
```

The pipeline runs each demo order one by one:

```
+===================================================================+
|   FOUNDRY — Enterprise E-Commerce Operations Center               |
+===================================================================+

  STEP 1: Fraud Detection .....................................
  STEP 2: Fulfillment Optimization ............................
  STEP 3: Customer Communication ..............................
```

---

## 📁 Project structure

```
FoundryCommerceDemo/
├── Program.cs                        # Entry point + pipeline
├── FoundryCommerceDemo.csproj        # Project file (.NET 10)
├── Models/
│   └── Order.cs                      # Order, LineItem, WarehouseStock
├── Data/
│   ├── OrderDatabase.cs              # Demo orders + customer history
│   └── WarehouseDatabase.cs          # Stock and shipping rates
├── Tools/
│   ├── FraudTools.cs                 # Geo + velocity + verdict
│   ├── scoring/
│   │   └── InventoryTools.cs         # Stock matrix, optimize, reserve
│   └── split-shipment/
│       └── CustomerTools.cs          # Customer email helpers
└── Agents/
    └── AgentDefinitions.cs           # Agent instructions
```

---

## 🧪 Demo orders

| Order | Customer | Total | What happens |
|-------|----------|-------|--------------|
| `ORD-50001` | Zeynep Arslan (Platinum) | $18,249 | ✅ APPROVED → split shipment (IST + FRA) |
| `ORD-50002` | John Smith (Standard) | $11,599 | 🚫 BLOCKED — country mismatch + too many orders + GPUs |
| `ORD-50003` | Maria Gonzalez (Gold) | $8,219 | ⚠️ APPROVED → smart stock plan |

### The "wow" moment — `ORD-50002`

- 💳 Card from: 🇺🇸 USA
- 🌐 IP from: 🇷🇴 Romania
- 📦 Ship to: 🇳🇬 Nigeria
- 🛒 **7 orders** in the last 24 hours + new device
- 🎮 4x RTX 5090 GPUs + a throwaway email
- → **Risk: CRITICAL → BLOCKED**
- The `CustomerAgent` writes a polite "we need more info" email.
  It does **not** mention the fraud reasons. ✨

---

## 🏠 Local mode (Ollama)

Do you want payment data to stay on your computer? (Good for PCI-DSS.)

```powershell
# 1. Install Ollama
winget install -e --id Ollama.Ollama
# or download from: https://ollama.com/download

# 2. Start the service
ollama serve

# 3. Pull a model that supports function calling
ollama pull llama3.1
```

In `Program.cs`, uncomment the local mode block and run again.
The same `Tools` and `Agents` code works — only the "brain" changes.

---

## 🩺 Troubleshooting

| Error | Reason | Fix |
|-------|--------|-----|
| `AuthenticationFailedException` | Token is expired | Run `az login` again |
| `404 — Model not found` | Model is not deployed | Foundry Portal → Models → deploy a model |
| `429 — Rate limited` | Too many requests | Wait 60 seconds or ask for more quota |
| `Insufficient permissions` | Missing role | Add the `Azure AI User` role |
| `Connection refused (Ollama)` | Ollama is not running | Run `ollama serve` in another terminal |
| `Wrong endpoint format` | Bad URL | Use `https://<resource>.services.ai.azure.com/api/projects/<project>` |

### Quick check commands

```powershell
az account show                       # Check Azure login
dotnet list package                   # See package versions
dotnet clean; dotnet build            # Clean and rebuild
curl http://localhost:11434/api/tags  # Check Ollama
```

---

## 📚 Resources

- [Microsoft Foundry Portal](https://foundry.azure.com)
- [Agent Framework on GitHub](https://github.com/microsoft/agent-framework)
- [Agent Framework Samples](https://github.com/microsoft/Agent-Framework-Samples)
- [Learn Docs — Overview](https://learn.microsoft.com/agent-framework/overview/)
- [Learn Docs — Workflows](https://learn.microsoft.com/agent-framework/workflows/)

---

## 📄 License

This demo is made for the **DOTNET Conference** talk. It is for learning only.

- All company names, customer names, and order data are **fictional**.
- The fraud detection logic is **simplified** for the demo.
  Please do **not** use it in production.

---

<sub>Built with Microsoft Agent Framework v1.0 — a unified SDK for enterprise AI agents.
Presented at DOTNET Conference · 7 May 2026 · Sheraton Grand Istanbul Hotel.</sub>
