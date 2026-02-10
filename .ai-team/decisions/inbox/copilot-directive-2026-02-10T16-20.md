### 2026-02-10: Brady's modernization decisions
**By:** bradygaster (via Copilot)
**What:**
1. SQL hosting: Docker for dev, Azure SQL for prod — approved
2. Deployment target: Azure Container Apps — confirmed
3. C# only — drop VB.NET SDK support entirely
4. Delete legacy code — no need to archive Client/, Server/, ClientWPF/, ServerMVC/ after migration
5. Use ALL original imagery — people who know .NET Terrarium should recognize it immediately
6. Cross-platform — change frontend to a web app instead of WPF desktop. Use .NET Aspire for orchestration. Staff up with new agents as needed. Use GitHub Issues and PRs to track all work items. Use labels for squad members and progress. Keep issues updated with status for GitHub project board tracking.
**Why:** Product owner decisions — these override Heisenberg's recommendations where they differ (especially #4 delete vs archive, #6 web app vs WPF)
