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
3. [What you need](#-what-you-need)
4. [Quick start](#-quick-start)
5. [Azure setup (`az login` step by step)](#-azure-setup-az-login-step-by-step)
6. [Environment variables](#-environment-variables)
7. [How to run](#-how-to-run)
8. [Project structure](#-project-structure)
9. [Demo orders](#-demo-orders)
10. [Local mode (Ollama)](#-local-mode-ollama)
11. [Troubleshooting](#-troubleshooting)
12. [License](#-license)

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
