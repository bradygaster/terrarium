# Terrarium Deployment Guide

This guide covers local development setup, Azure deployment via Azure Developer CLI (`azd`), and production configuration for the Terrarium game ecosystem.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Azure Deployment](#azure-deployment)
4. [Custom Domain Configuration](#custom-domain-configuration)
5. [Environment Variables and Configuration](#environment-variables-and-configuration)
6. [Monitoring and Application Insights](#monitoring-and-application-insights)
7. [GitHub Actions CD Pipeline](#github-actions-cd-pipeline)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before deploying Terrarium, ensure you have the following tools installed:

### Required Tools

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| **.NET SDK** | 10.0 or later | Build and run the application | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Azure CLI** | Latest | Manage Azure resources | [Download](https://learn.microsoft.com/cli/azure/install-azure-cli) |
| **Azure Developer CLI (azd)** | Latest | Deploy to Azure with one command | [Download](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) |
| **Docker** | Latest | Run local containers and build images | [Download](https://www.docker.com/products/docker-desktop) |
| **Git** | Latest | Source control | [Download](https://git-scm.com/) |

### Optional Tools

- **Visual Studio 2022** (17.11+) or **Visual Studio Code** with C# Dev Kit
- **Azure Storage Explorer** (for diagnostic logs)

### Verify Installation

```bash
# Check .NET SDK
dotnet --version
# Expected: 10.0.x or later

# Check Azure CLI
az --version
# Expected: azure-cli 2.x.x

# Check Azure Developer CLI
azd version
# Expected: azd version x.x.x

# Check Docker
docker --version
# Expected: Docker version 20.x.x or later
```

---

## Local Development

Terrarium uses [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) for local orchestration, providing a unified developer experience with automatic resource provisioning and health monitoring.

### Quick Start

1. **Clone the repository:**

   ```bash
   git clone https://github.com/[your-org]/terrarium.git
   cd terrarium
   ```

2. **Run the Aspire AppHost:**

   ```bash
   dotnet run --project src/Terrarium.AppHost
   ```

   This command:
   - Starts the Aspire Dashboard
   - Provisions local SQL Server (Docker container)
   - Provisions Azure SignalR Service emulator (Docker container)
   - Starts `Terrarium.Server` and `Terrarium.Web` projects
   - Wires up dependency references and connection strings

3. **Access the application:**

   The Aspire Dashboard will display URLs for all running resources. Typically:
   - **Aspire Dashboard**: `http://localhost:15000` (shows logs, traces, metrics, and health)
   - **Terrarium Web**: `http://localhost:5xxx` (Blazor frontend)
   - **Terrarium Server**: `http://localhost:5yyy` (API backend)

   The exact ports are dynamically allocated by Aspire and shown in the dashboard.

### What Runs Locally?

| Resource | Local Provider | Purpose |
|----------|----------------|---------|
| **SQL Server** | Docker (`mcr.microsoft.com/mssql/server`) | Game database (species, creatures, peers) |
| **Azure SignalR** | Emulator (Docker) | WebSocket backplane for real-time updates |
| **Terrarium.Server** | .NET process | API backend (peer discovery, registration) |
| **Terrarium.Web** | .NET process | Blazor frontend (ecosystem visualization) |

### Local Configuration

Aspire automatically generates connection strings and injects them into your apps. No manual configuration is needed for local development.

If you need to customize local settings:

1. **User secrets** (recommended for sensitive values):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Terrarium" "your-connection-string" --project src/Terrarium.Server
   ```

2. **appsettings.Development.json** (for non-sensitive overrides):

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

### Stopping Local Resources

Press `Ctrl+C` in the terminal running `dotnet run`. Aspire will gracefully shut down all resources.

To clean up Docker containers:

```bash
docker ps -a | grep terrarium | awk '{print $1}' | xargs docker rm -f
```

---

## Azure Deployment

Terrarium is designed for Azure deployment using the Azure Developer CLI (`azd`). The infrastructure is defined as Bicep templates in the `infra/` directory.

### Architecture Overview

**Deployed resources:**

| Resource | Azure Service | Purpose |
|----------|--------------|---------|
| **Container Apps Environment** | Azure Container Apps | Managed environment for both apps |
| **Log Analytics Workspace** | Azure Monitor | Centralized logging for all resources |
| **Terrarium.Server** | Container App | API backend (internal ingress, sticky sessions) |
| **Terrarium.Web** | Container App | Blazor frontend (external ingress, public endpoint) |
| **Azure SQL Database** | Azure SQL (Basic tier) | Game database (species, creatures, world state) |
| **Azure SignalR Service** | SignalR Service (Standard S1) | WebSocket backplane for multi-server scaling |

### Deployment Steps

#### 1. Initialize Azure Developer CLI

If this is your first deployment, initialize `azd`:

```bash
azd init
```

**Note**: The repository already contains `azure.yaml`, so `azd init` will detect the existing configuration. You can skip this step if `azure.yaml` is already present.

#### 2. Authenticate with Azure

```bash
azd auth login
```

This opens a browser window for Azure authentication. Sign in with an account that has contributor access to the target subscription.

Verify authentication:

```bash
az account show
```

If you need to switch subscriptions:

```bash
az account set --subscription "Your Subscription Name"
```

#### 3. Deploy to Azure

```bash
azd up
```

This command performs the following steps:

1. **Package** — Builds Docker images for `Terrarium.Server` and `Terrarium.Web`
2. **Provision** — Creates Azure resources defined in `infra/main.bicep`
3. **Deploy** — Pushes container images to Azure and updates Container Apps

**Interactive prompts:**

- **Environment name**: A unique identifier for this deployment (e.g., `terrarium-prod`)
- **Azure location**: Region for all resources (e.g., `eastus`, `westus2`)
- **SQL admin password**: Secure password for Azure SQL Server (stored in `azd` environment)

**First deployment time**: ~5-8 minutes

**Subsequent deployments**: ~2-3 minutes (only updates changed resources)

#### 4. Verify Deployment

After `azd up` completes, you'll see output URLs:

```
SUCCESS: Your application was provisioned and deployed to Azure in X minutes Y seconds.

Endpoints:
- Web: https://web-<environment-name>.azurecontainerapps.io
- Server: https://server-<environment-name>.azurecontainerapps.io
```

Open the **Web URL** in a browser to verify the deployment.

### Deployment Environments

You can create multiple environments (dev, staging, prod) using `azd`:

```bash
# Create a new environment
azd env new terrarium-staging

# Deploy to the new environment
azd up

# Switch between environments
azd env select terrarium-prod
azd up
```

Each environment maintains separate:
- Azure resources
- Environment variables
- Secrets (SQL password, connection strings)

### Updating an Existing Deployment

To deploy code changes without reprovisioning infrastructure:

```bash
azd deploy
```

This skips the Bicep provisioning step and only updates the container images.

To reprovision infrastructure (after modifying `infra/main.bicep`):

```bash
azd provision
```

### Viewing Deployment Status

Check resource health in the Azure Portal:

```bash
azd show
```

This opens the Azure Portal to the resource group for the selected environment.

Alternatively, view logs directly:

```bash
# Stream logs from Terrarium.Server
az containerapp logs show --name server-<environment-name> --resource-group <resource-group> --follow

# Stream logs from Terrarium.Web
az containerapp logs show --name web-<environment-name> --resource-group <resource-group> --follow
```

---

## Custom Domain Configuration

By default, Container Apps are accessible via Azure-provided URLs (`*.azurecontainerapps.io`). To use a custom domain with SSL:

### Prerequisites

- A registered domain (e.g., `terrarium.example.com`)
- Access to DNS management for the domain

### Steps

#### 1. Add Custom Domain to Container App

```bash
az containerapp hostname add \
  --hostname terrarium.example.com \
  --name web-<environment-name> \
  --resource-group <resource-group>
```

#### 2. Retrieve Validation Token

```bash
az containerapp hostname list \
  --name web-<environment-name> \
  --resource-group <resource-group> \
  --query "[?name=='terrarium.example.com'].{Name:name,ValidationToken:validationToken}" \
  --output table
```

#### 3. Configure DNS

Add the following DNS records to your domain:

**A Record** (or CNAME):
```
terrarium.example.com → <Container App IP or FQDN>
```

**TXT Record** (for domain validation):
```
asuid.terrarium.example.com → <ValidationToken>
```

#### 4. Bind SSL Certificate

Azure Container Apps automatically provisions a free managed certificate once DNS validation succeeds.

Verify the certificate:

```bash
az containerapp hostname list \
  --name web-<environment-name> \
  --resource-group <resource-group>
```

Look for `bindingType: "SniEnabled"` and `certificateId: "<cert-id>"`.

#### 5. Test

Open `https://terrarium.example.com` in a browser. You should see a valid SSL certificate (green padlock).

### Custom Domain for Internal Services

The `Terrarium.Server` Container App uses **internal ingress** (not publicly accessible). If you need to expose it:

1. Update `infra/main.bicep` to set `external: true` for the server app's ingress configuration
2. Redeploy: `azd provision && azd deploy`
3. Follow the same custom domain steps for the server app

**Security note**: Internal ingress is recommended for backend APIs to reduce attack surface.

---

## Environment Variables and Configuration

Terrarium uses the [.NET configuration system](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/) with support for environment variables, secrets, and Azure App Configuration.

### Key Configuration Values

#### Terrarium.Server

| Setting | Environment Variable | Description | Example |
|---------|---------------------|-------------|---------|
| **SQL Connection String** | `ConnectionStrings__Terrarium` | Azure SQL Database connection | `Server=tcp:sql-...database.windows.net,1433;Database=Terrarium;...` |
| **SignalR Connection String** | `ConnectionStrings__signalr` | Azure SignalR Service connection | `Endpoint=https://signalr-....signalr.net;AccessKey=...;Version=1.0;` |
| **Logging Level** | `Logging__LogLevel__Default` | Minimum log level | `Information` |

#### Terrarium.Web

| Setting | Environment Variable | Description | Example |
|---------|---------------------|-------------|---------|
| **Server URL** | `services__server__https__0` | URL of Terrarium.Server | `https://server-<env>.azurecontainerapps.io` |
| **Logging Level** | `Logging__LogLevel__Default` | Minimum log level | `Information` |

### Setting Environment Variables in Azure

Environment variables are injected into Container Apps via Bicep. To update a variable:

#### Option 1: Update Bicep and Redeploy

Edit `infra/main.bicep`:

```bicep
env: [
  {
    name: 'MyNewSetting'
    value: 'MyValue'
  }
]
```

Then redeploy:

```bash
azd provision
```

#### Option 2: Update via Azure CLI

```bash
az containerapp update \
  --name server-<environment-name> \
  --resource-group <resource-group> \
  --set-env-vars "MyNewSetting=MyValue"
```

This immediately updates the Container App without redeploying code.

### Secrets Management

Sensitive values (SQL password, SignalR connection string) are stored as **Container App secrets** in Bicep:

```bicep
secrets: [
  {
    name: 'sql-connection'
    value: 'Server=tcp:...'
  }
]

env: [
  {
    name: 'ConnectionStrings__Terrarium'
    secretRef: 'sql-connection'
  }
]
```

Secrets are encrypted at rest and never appear in logs.

To rotate a secret:

```bash
az containerapp secret set \
  --name server-<environment-name> \
  --resource-group <resource-group> \
  --secrets sql-connection="<new-connection-string>"
```

The Container App automatically restarts to pick up the new secret.

### Production Configuration Checklist

Before deploying to production:

- ✅ SQL Database uses a strong admin password (min 12 characters, mixed case, numbers, symbols)
- ✅ SQL firewall rules are restricted (remove `AllowAllAzureIps` if not needed)
- ✅ SignalR Service is set to `Standard_S1` or higher for production SLA
- ✅ Container Apps have appropriate CPU/memory resource limits
- ✅ Logging level is set to `Information` or `Warning` (not `Debug`)
- ✅ Health probes are configured correctly (see `docs/deployment/health-probes.md`)
- ✅ Auto-scaling rules are tuned for expected traffic

---

## Monitoring and Application Insights

Terrarium uses **Azure Monitor** and **Application Insights** for observability.

### What's Monitored?

| Telemetry Type | Source | Destination |
|----------------|--------|-------------|
| **Logs** | Container Apps (stdout/stderr) | Log Analytics Workspace |
| **Traces** | ASP.NET Core activity traces | Application Insights |
| **Metrics** | Container Apps (CPU, memory, HTTP) | Azure Monitor |
| **Dependencies** | SQL, SignalR, HTTP calls | Application Insights |

### Accessing Logs

#### Azure Portal

1. Navigate to the **Container App** (e.g., `server-<environment-name>`)
2. Select **Logs** in the left menu
3. Run a KQL query:

   ```kql
   ContainerAppConsoleLogs_CL
   | where ContainerName_s == "server"
   | project TimeGenerated, Log_s
   | order by TimeGenerated desc
   | take 100
   ```

#### Azure CLI

```bash
# Stream live logs
az containerapp logs show --name server-<environment-name> --resource-group <resource-group> --follow

# Tail logs (last 100 lines)
az containerapp logs show --name server-<environment-name> --resource-group <resource-group> --tail 100
```

### Application Insights

Application Insights is automatically configured via the `Terrarium.ServiceDefaults` library.

**Key features:**

- **Live Metrics**: Real-time telemetry (CPU, memory, requests/sec)
- **Failures**: Exception tracking with stack traces
- **Performance**: Request duration and dependency latency
- **Application Map**: Visualize service dependencies (Web → Server → SQL → SignalR)

**Accessing Application Insights:**

1. Navigate to the **Log Analytics Workspace** (`log-<environment-name>`)
2. Select **Application Insights** in the left menu
3. Explore:
   - **Live Metrics** — Real-time performance
   - **Failures** — Exceptions and failed requests
   - **Performance** — Slow requests and dependencies
   - **Application Map** — Service topology

### Health Check Monitoring

Terrarium exposes health check endpoints for Container Apps probes:

- `/alive` — Liveness check (basic self-check)
- `/health` — Readiness check (full dependency check)

**See detailed health probe configuration**: `docs/deployment/health-probes.md`

To query health check results in Log Analytics:

```kql
ContainerAppConsoleLogs_CL
| where Log_s contains "Health check"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```

### Setting Up Alerts

Create an alert when Terrarium.Server fails health checks:

```bash
az monitor metrics alert create \
  --name "Terrarium Server Health Alert" \
  --resource-group <resource-group> \
  --scopes /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.App/containerApps/server-<env> \
  --condition "avg Percentage CPU > 80" \
  --description "Triggered when server CPU exceeds 80%" \
  --evaluation-frequency 1m \
  --window-size 5m \
  --severity 2
```

For readiness probe failures:

```bash
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where Log_s contains 'Readiness probe failed'" \
  --timespan PT1H
```

### Monitoring Auto-Scaling

Check current replica count:

```bash
az containerapp replica list \
  --name server-<environment-name> \
  --resource-group <resource-group>
```

View scaling events in Log Analytics:

```kql
ContainerAppSystemLogs_CL
| where Log_s contains "scaled"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```

---

## GitHub Actions CD Pipeline

Automate deployment to Azure using GitHub Actions. The workflow builds, tests, and deploys Terrarium on every push to `main`.

### Workflow File

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches:
      - main
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    name: Build and Deploy
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore src/Terrarium.sln
      
      - name: Build
        run: dotnet build src/Terrarium.sln --configuration Release --no-restore
      
      - name: Test
        run: dotnet test src/Terrarium.sln --configuration Release --no-build --verbosity normal
      
      - name: Install azd
        uses: Azure/setup-azd@v1.0.0
      
      - name: Azure Login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      
      - name: Deploy to Azure
        run: azd deploy --no-prompt
        env:
          AZURE_ENV_NAME: ${{ vars.AZURE_ENV_NAME }}
          AZURE_LOCATION: ${{ vars.AZURE_LOCATION }}
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Required Secrets and Variables

Configure these in **GitHub Settings → Secrets and variables → Actions**:

#### Secrets

| Secret | Description | How to Get |
|--------|-------------|------------|
| `AZURE_CLIENT_ID` | Service Principal client ID | `az ad sp create-for-rbac --name terrarium-github-deploy` |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `az account show --query id -o tsv` |

#### Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_ENV_NAME` | Environment name for deployment | `terrarium-prod` |
| `AZURE_LOCATION` | Azure region | `eastus` |

### Setting Up OIDC Authentication

GitHub Actions uses OpenID Connect (OIDC) for passwordless authentication to Azure.

#### 1. Create a Service Principal

```bash
az ad sp create-for-rbac --name terrarium-github-deploy --role contributor --scopes /subscriptions/<subscription-id>/resourceGroups/<resource-group> --sdk-auth
```

Copy the output JSON and save:
- `clientId` → `AZURE_CLIENT_ID` secret
- `tenantId` → `AZURE_TENANT_ID` secret
- `subscriptionId` → `AZURE_SUBSCRIPTION_ID` secret

#### 2. Configure Federated Credentials

```bash
az ad app federated-credential create \
  --id <app-id> \
  --parameters '{
    "name": "terrarium-github-deploy",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<your-org>/terrarium:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### 3. Test the Workflow

Push a commit to `main`:

```bash
git commit --allow-empty -m "Test deployment workflow"
git push origin main
```

Watch the workflow run in **GitHub Actions** tab.

### Manual Deployment Trigger

The workflow includes `workflow_dispatch`, allowing manual runs:

1. Navigate to **Actions** → **Deploy to Azure**
2. Click **Run workflow**
3. Select branch (default: `main`)
4. Click **Run workflow**

### Environment-Specific Deployments

To deploy to multiple environments (dev, staging, prod), create separate workflows or use matrix builds:

```yaml
strategy:
  matrix:
    environment: [dev, staging, prod]

env:
  AZURE_ENV_NAME: terrarium-${{ matrix.environment }}
```

---

## Troubleshooting

### Common Issues

#### Issue: `azd up` fails with "resource already exists"

**Cause**: You're trying to deploy to an environment that already has resources.

**Solution**:
```bash
# Delete the existing environment
azd down --force --purge

# Redeploy
azd up
```

#### Issue: Container App stuck in "Provisioning" state

**Cause**: Health probes are failing, preventing the container from starting.

**Solution**:
1. Check logs:
   ```bash
   az containerapp logs show --name server-<env> --resource-group <rg> --tail 100
   ```
2. Common causes:
   - SQL connection string is incorrect
   - SignalR connection string is incorrect
   - Health probe timeout is too short

3. Verify health probes manually:
   ```bash
   curl https://server-<env>.azurecontainerapps.io/alive
   curl https://server-<env>.azurecontainerapps.io/health
   ```

#### Issue: SQL connection fails with "login failed"

**Cause**: SQL firewall is blocking the Container App IP, or credentials are incorrect.

**Solution**:
1. Verify SQL firewall allows Azure services:
   ```bash
   az sql server firewall-rule show \
     --server sql-<env> \
     --resource-group <rg> \
     --name AllowAllAzureIps
   ```
2. If missing, add the rule:
   ```bash
   az sql server firewall-rule create \
     --server sql-<env> \
     --resource-group <rg> \
     --name AllowAllAzureIps \
     --start-ip-address 0.0.0.0 \
     --end-ip-address 0.0.0.0
   ```
3. Verify credentials by connecting with `sqlcmd` or Azure Data Studio.

#### Issue: SignalR connections fail

**Cause**: SignalR connection string is missing or incorrect.

**Solution**:
1. Retrieve the SignalR connection string:
   ```bash
   az signalr key list \
     --name signalr-<env> \
     --resource-group <rg> \
     --query primaryConnectionString -o tsv
   ```
2. Update the Container App secret:
   ```bash
   az containerapp secret set \
     --name server-<env> \
     --resource-group <rg> \
     --secrets signalr-connection="<connection-string>"
   ```
3. Restart the Container App to pick up the new secret.

#### Issue: Deployment is slow

**Cause**: Container image build takes time, especially on first deployment.

**Solution**:
- First deployment: 5-8 minutes (normal)
- Subsequent deployments: 2-3 minutes
- To speed up: Use GitHub Actions caching for Docker layers

#### Issue: GitHub Actions workflow fails with "azd: command not found"

**Cause**: `azd` CLI is not installed in the GitHub Actions runner.

**Solution**:
Ensure the workflow includes:
```yaml
- name: Install azd
  uses: Azure/setup-azd@v1.0.0
```

#### Issue: Health checks pass locally but fail in Azure

**Cause**: Local environment uses different ports or URLs.

**Solution**:
1. Verify the health check paths in `infra/main.bicep` match your app's configuration
2. Check that the Container App is listening on port `8080` (configured in Bicep)
3. Verify `Terrarium.ServiceDefaults` registers health checks correctly

#### Issue: Auto-scaling doesn't trigger

**Cause**: Metrics threshold not reached, or scaling rules misconfigured.

**Solution**:
1. Check current metrics:
   ```bash
   az monitor metrics list \
     --resource /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.App/containerApps/server-<env> \
     --metric "Percentage CPU"
   ```
2. Lower the threshold temporarily to test:
   ```bicep
   metadata: {
     type: 'Utilization'
     value: '30'  // Lower threshold for testing
   }
   ```
3. Generate load and observe scaling in Azure Portal

### Getting Help

- **Azure Container Apps Docs**: https://learn.microsoft.com/azure/container-apps/
- **Aspire Docs**: https://learn.microsoft.com/dotnet/aspire/
- **Azure Developer CLI Docs**: https://learn.microsoft.com/azure/developer/azure-developer-cli/
- **Terrarium Issues**: https://github.com/[your-org]/terrarium/issues

### Diagnostic Commands

```bash
# Check Container App status
az containerapp show --name server-<env> --resource-group <rg> --query "properties.provisioningState"

# View recent revisions
az containerapp revision list --name server-<env> --resource-group <rg> --output table

# Check health probe configuration
az containerapp show --name server-<env> --resource-group <rg> --query "properties.template.containers[0].probes"

# View environment variables
az containerapp show --name server-<env> --resource-group <rg> --query "properties.template.containers[0].env"

# Check scaling rules
az containerapp show --name server-<env> --resource-group <rg> --query "properties.template.scale"
```

---

## Next Steps

- **Load Testing**: Use Azure Load Testing to verify scaling behavior under load
- **Backup Strategy**: Configure automated SQL Database backups
- **Disaster Recovery**: Set up geo-replication for SQL and multi-region deployment
- **Cost Optimization**: Review Azure Cost Management recommendations for Container Apps and SignalR

For more information:
- [Health Probes and Auto-Scaling](./health-probes.md)
- [Deployment Checklist](./checklist.md)
- [Architecture Documentation](../ARCHITECTURE.md)
