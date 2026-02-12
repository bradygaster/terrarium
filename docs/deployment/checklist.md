# Terrarium Production Deployment Checklist

This checklist ensures all requirements are met before deploying Terrarium to production.

## Pre-Deployment

### Infrastructure Verification

- [ ] **Azure subscription** is active with sufficient quota for Container Apps, SQL, and SignalR
- [ ] **Resource group** exists or will be created by `azd` (name: `rg-<environment-name>`)
- [ ] **Azure region** is selected (recommended: `eastus`, `westus2`, `northeurope`)
- [ ] **Environment name** is chosen (e.g., `terrarium-prod`) and documented
- [ ] **Bicep templates** in `infra/main.bicep` are reviewed and tested

### Authentication & Permissions

- [ ] **Azure CLI** is authenticated: `az account show`
- [ ] **Azure Developer CLI** is authenticated: `azd auth login`
- [ ] Service principal has **Contributor** role on the resource group (for GitHub Actions)
- [ ] OIDC federated credentials are configured (for GitHub Actions passwordless auth)

### Code Readiness

- [ ] **Build succeeds** locally: `dotnet build src/Terrarium.sln --configuration Release`
- [ ] **All tests pass** locally: `dotnet test src/Terrarium.sln --configuration Release`
- [ ] **Code is merged** to `main` branch (or the branch you're deploying from)
- [ ] **Git tags** are applied for version tracking (e.g., `v1.0.0`)

### Configuration

- [ ] **SQL admin password** is generated (strong: min 12 chars, mixed case, numbers, symbols)
- [ ] **Environment variables** are documented (see `deployment-guide.md`)
- [ ] **Secrets** are prepared (SQL password, SignalR connection string will be auto-generated)
- [ ] **Logging level** is set appropriately (`Information` or `Warning` for production, not `Debug`)

### Documentation

- [ ] **Deployment guide** (`docs/deployment/deployment-guide.md`) is reviewed
- [ ] **Health probes** (`docs/deployment/health-probes.md`) are understood
- [ ] **Runbook** for incident response is prepared (who to contact, escalation paths)
- [ ] **Architecture docs** (`ARCHITECTURE.md`) are up-to-date

---

## Deployment

### Initial Deployment (First Time)

- [ ] **Run `azd init`** (if not already done — skip if `azure.yaml` exists)
  ```bash
  azd init
  ```
- [ ] **Run `azd up`** to provision and deploy
  ```bash
  azd up
  ```
- [ ] **Provide inputs** when prompted:
  - Environment name: `terrarium-prod`
  - Azure location: `eastus`
  - SQL admin password: `<secure-password>`
- [ ] **Wait for deployment** (first deployment: 5-8 minutes)
- [ ] **Record outputs**:
  - Web URL: `https://web-<env>.azurecontainerapps.io`
  - Server URL: `https://server-<env>.azurecontainerapps.io`

### Subsequent Deployments (Code Updates)

- [ ] **Run `azd deploy`** to update containers without reprovisioning
  ```bash
  azd deploy --no-prompt
  ```
- [ ] **Wait for deployment** (2-3 minutes)

### Infrastructure Changes (Bicep Updates)

- [ ] **Test Bicep changes** locally:
  ```bash
  az deployment group validate \
    --resource-group <rg> \
    --template-file infra/main.bicep \
    --parameters environmentName=<env> sqlAdminPassword=<pwd> serverImage=<img> webImage=<img>
  ```
- [ ] **Run `azd provision`** to apply infrastructure changes
  ```bash
  azd provision
  ```
- [ ] **Run `azd deploy`** to update code
  ```bash
  azd deploy --no-prompt
  ```

---

## Post-Deployment Verification

### Health Checks

- [ ] **Web app is accessible**: Open `https://web-<env>.azurecontainerapps.io` in a browser
- [ ] **Home page loads** without errors (Blazor app initializes)
- [ ] **Server health endpoint** responds: `curl https://server-<env>.azurecontainerapps.io/health`
- [ ] **Liveness endpoint** responds: `curl https://server-<env>.azurecontainerapps.io/alive`
- [ ] **No 503 errors** (if 503, health probes are failing — check logs)

### Container App Status

- [ ] **Server Container App** is `Running`:
  ```bash
  az containerapp show --name server-<env> --resource-group <rg> --query "properties.runningStatus"
  ```
- [ ] **Web Container App** is `Running`:
  ```bash
  az containerapp show --name web-<env> --resource-group <rg> --query "properties.runningStatus"
  ```
- [ ] **At least 1 replica** is running for each app:
  ```bash
  az containerapp replica list --name server-<env> --resource-group <rg>
  ```

### Database Connectivity

- [ ] **SQL Database** is provisioned and accessible
- [ ] **Connection string** is correct in Container App secrets
- [ ] **Firewall rule** allows Container Apps (Azure services)
- [ ] **Test query** succeeds (connect via Azure Data Studio or `sqlcmd`)

### SignalR Service

- [ ] **SignalR Service** is provisioned (`Standard_S1` tier for production)
- [ ] **Connection string** is correct in Container App secrets
- [ ] **CORS** is configured (allows web app origin)
- [ ] **Connectivity logs** are enabled (for troubleshooting)

### Logging and Monitoring

- [ ] **Log Analytics Workspace** is receiving logs:
  ```bash
  az monitor log-analytics query \
    --workspace <workspace-id> \
    --analytics-query "ContainerAppConsoleLogs_CL | take 10"
  ```
- [ ] **Application Insights** is collecting telemetry (check Azure Portal)
- [ ] **Live Metrics** shows real-time data (Application Insights → Live Metrics)
- [ ] **No exceptions** in the last 10 minutes (Application Insights → Failures)

### Auto-Scaling

- [ ] **Scaling rules** are configured in Bicep (`infra/main.bicep`)
- [ ] **Server scaling**:
  - Min replicas: 1
  - Max replicas: 10
  - Rules: CPU (70%) + SignalR connections (100)
- [ ] **Web scaling**:
  - Min replicas: 1
  - Max replicas: 5
  - Rules: HTTP requests (50 concurrent)
- [ ] **Scaling events** are logged (check Log Analytics for "scaled" messages)

### Security

- [ ] **SQL Server firewall** is restricted (only Azure services, or specific IPs)
- [ ] **Secrets** are stored securely in Container App secrets (not environment variables)
- [ ] **HTTPS** is enforced (all traffic over TLS)
- [ ] **Managed identities** are used where possible (not yet implemented, future enhancement)
- [ ] **No secrets** are committed to Git (check `.gitignore`)

---

## GitHub Actions (CI/CD)

### Workflow Configuration

- [ ] **Workflow file** exists: `.github/workflows/deploy.yml`
- [ ] **Triggers** are configured (push to `main`, or `workflow_dispatch`)
- [ ] **Build and test** steps are included
- [ ] **`azd deploy`** step is included

### Secrets and Variables

- [ ] **GitHub Secrets** are configured:
  - `AZURE_CLIENT_ID`
  - `AZURE_TENANT_ID`
  - `AZURE_SUBSCRIPTION_ID`
- [ ] **GitHub Variables** are configured:
  - `AZURE_ENV_NAME` (e.g., `terrarium-prod`)
  - `AZURE_LOCATION` (e.g., `eastus`)

### OIDC Authentication

- [ ] **Service Principal** is created with Contributor role
- [ ] **Federated credentials** are configured for GitHub Actions
- [ ] **OIDC authentication** works (test by running the workflow)

### Workflow Verification

- [ ] **Workflow runs** successfully on push to `main`
- [ ] **Build step** passes (no compilation errors)
- [ ] **Test step** passes (all tests green)
- [ ] **Deploy step** completes (resources are updated)
- [ ] **No failures** in the last 3 runs

---

## Custom Domain (Optional)

- [ ] **Domain is registered** (e.g., `terrarium.example.com`)
- [ ] **DNS access** is available (ability to add A/CNAME/TXT records)
- [ ] **Custom domain is added** to Container App:
  ```bash
  az containerapp hostname add --hostname terrarium.example.com --name web-<env> --resource-group <rg>
  ```
- [ ] **DNS records are configured**:
  - A or CNAME record points to Container App FQDN
  - TXT record for domain validation (`asuid.terrarium.example.com`)
- [ ] **Managed certificate** is provisioned (wait 5-10 minutes after DNS config)
- [ ] **HTTPS works** on custom domain (green padlock in browser)

---

## Rollback Plan

- [ ] **Previous revision** is documented (revision ID from Azure Portal)
- [ ] **Rollback command** is prepared:
  ```bash
  az containerapp revision activate \
    --name server-<env> \
    --resource-group <rg> \
    --revision <previous-revision-id>
  ```
- [ ] **Rollback test** is performed in a staging environment (optional but recommended)

---

## Monitoring and Alerts

### Alerts Configuration

- [ ] **CPU alert** is configured (trigger when avg CPU > 80%)
- [ ] **Memory alert** is configured (trigger when memory > 90%)
- [ ] **Health probe failure alert** is configured
- [ ] **SQL DTU alert** is configured (if using SQL Database Basic tier)
- [ ] **SignalR connection alert** is configured (if connection count is critical)

### Alert Channels

- [ ] **Email notifications** are configured (Action Group with email recipients)
- [ ] **Slack/Teams integration** is configured (optional, for faster response)
- [ ] **PagerDuty integration** is configured (optional, for 24/7 on-call)

### Dashboard

- [ ] **Azure Dashboard** is created with key metrics:
  - Container App CPU/memory usage
  - HTTP request rate
  - SQL DTU usage
  - SignalR connection count
  - Error rate
- [ ] **Dashboard is shared** with the team (for visibility)

---

## Documentation Updates

- [ ] **README.md** includes deployment instructions or links to this guide
- [ ] **ARCHITECTURE.md** is updated with production topology
- [ ] **Environment URLs** are documented (dev, staging, prod)
- [ ] **Contact information** is documented (who to contact for incidents)
- [ ] **Changelog** is updated with the deployment date and version

---

## Post-Deployment Tasks

### Day 1

- [ ] **Monitor logs** for the first 24 hours (check for exceptions, errors)
- [ ] **Load test** the application (simulate expected user load)
- [ ] **Verify auto-scaling** triggers under load (generate traffic and observe replicas)
- [ ] **Check cost** in Azure Cost Management (ensure it's within budget)

### Week 1

- [ ] **Review telemetry** in Application Insights (request duration, dependency latency)
- [ ] **Optimize scaling rules** if needed (adjust thresholds based on real traffic)
- [ ] **Review logs** for warning patterns (repeated errors, slow queries)
- [ ] **Plan next release** (features, bug fixes)

### Ongoing

- [ ] **Weekly health checks** (review monitoring dashboard, check for alerts)
- [ ] **Monthly cost review** (optimize resource usage)
- [ ] **Quarterly load tests** (ensure scaling works as traffic grows)
- [ ] **Disaster recovery test** (restore from backup, failover to secondary region)

---

## Sign-Off

### Deployment Lead

- Name: _______________________________
- Date: _______________________________
- Signature: _______________________________

### Approval

- Product Owner: _______________________________
- Engineering Manager: _______________________________
- Operations Manager: _______________________________

---

## Notes

Use this section to document any deployment-specific notes, issues encountered, or deviations from the checklist.

```
[Add notes here]
```

---

## Related Documentation

- [Deployment Guide](./deployment-guide.md) — Comprehensive deployment instructions
- [Health Probes](./health-probes.md) — Health check configuration and auto-scaling
- [Architecture](../ARCHITECTURE.md) — System architecture and design decisions
- [Azure Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI Docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/)

---

**Last Updated**: [Date]  
**Reviewed By**: [Name]
