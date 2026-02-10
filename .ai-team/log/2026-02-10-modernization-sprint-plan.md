# Session: Modernization Sprint Plan

- **Date:** 2026-02-10
- **Requested by:** bradygaster

## What happened

Heisenberg created a 14-sprint (~7 month) .NET 10 modernization plan for Terrarium (documented in `MODERNIZATION.md`). The plan migrates from .NET Framework 3.5 to .NET 10, builds leaf-to-root, and parallelizes server and client work.

## Key architectural decisions

| Decision | Choice |
|----------|--------|
| UI framework | WPF on .NET 10 |
| Graphics | Silk.NET OpenGL (replacing DirectX 7 DirectDraw) |
| P2P networking | gRPC (replacing custom TCP) |
| Database access | Dapper + existing stored procedures (not EF Core) |
| Server | ASP.NET Core Minimal APIs (replacing ASMX) |
| Security sandboxing | AssemblyLoadContext + process isolation (replacing CAS) |
| Serialization | System.Text.Json (replacing BinaryFormatter) |
| Test framework | xUnit |
| IL validation | System.Reflection.Metadata (replacing native C++ AsmCheck) |
| Solution structure | New SDK-style `.csproj` files (not migrating legacy) |

## Decisions flagged for Brady's input

1. SQL Server hosting (Azure SQL / Docker / LocalDB)
2. Deployment target (App Service / Container Apps / Self-hosted)
3. VB.NET SDK support (keep or drop)
4. Legacy code disposition (delete or archive `ClientWPF/`, `ServerMVC/`)
5. Sprite assets (originals, new art, or placeholder)
6. Cross-platform aspirations (Windows-only WPF vs. future Avalonia)

## Agents involved

- **Heisenberg** — authored the plan
