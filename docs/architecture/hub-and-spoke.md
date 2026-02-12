# Hub-and-Spoke Architecture

> Terrarium .NET 10 — Orleans grain architecture for real-time ecosystem simulation
>
> **See also:** [SignalR Hub-and-Spoke Detailed Design](signalr-hub-spoke.md) — full protocol specification, message contracts, error handling, and migration guide.

## Overview

The hub-and-spoke architecture places **Orleans virtual actors (grains)** at the center of all stateful game logic. **SignalR** serves as a thin browser push channel only — it relays messages between clients and grains but holds no domain state.

```mermaid
graph TB
    subgraph Clients
        B1[Browser 1]
        B2[Browser 2]
        B3[Browser N]
    end

    subgraph Server ["Terrarium.Server (ASP.NET Core)"]
        Hub[TerrariumHub<br/>SignalR Hub]
    end

    subgraph Orleans ["Orleans Silo"]
        EG[EcosystemGrain<br/>World State + Tick]
        PG[PeerGrain<br/>Per-Peer State]
        SR[SpeciesRegistryGrain<br/>Global Species Catalog]
        POP[PopulationGrain<br/>Population History]
    end

    B1 <-->|SignalR WebSocket| Hub
    B2 <-->|SignalR WebSocket| Hub
    B3 <-->|SignalR WebSocket| Hub

    Hub -->|GetWorldState / ProcessTick| EG
    Hub -->|Register / Heartbeat| PG
    Hub -->|RegisterSpecies / Query| SR
    Hub -->|RecordSnapshot / GetHistory| POP

    EG -->|Tick notifications| Hub
    EG -->|Record population| POP
```

## Grain Responsibilities

```mermaid
classDiagram
    class IEcosystemGrain {
        <<interface>>
        +GetWorldStateAsync() WorldSnapshot
        +ProcessTickAsync()
        +AddOrganismAsync(speciesId, assemblyData)
        +RemoveOrganismAsync(organismId)
        +GetCreatureCountAsync() int
    }

    class IPeerGrain {
        <<interface>>
        +RegisterAsync(endpoint, version)
        +HeartbeatAsync()
        +GetInfoAsync() PeerInfo
    }

    class ISpeciesRegistryGrain {
        <<interface>>
        +RegisterSpeciesAsync(name, assembly)
        +GetAllSpeciesAsync() IReadOnlyList~SpeciesInfo~
        +BlacklistSpeciesAsync(name, reason)
    }

    class IPopulationGrain {
        <<interface>>
        +RecordSnapshotAsync(snapshot)
        +GetHistoryAsync(limit) IReadOnlyList~PopulationSnapshot~
    }
```

| Grain | Key | Cardinality | Purpose |
|-------|-----|-------------|---------|
| **EcosystemGrain** | Ecosystem ID | One per ecosystem | Owns world state, processes ticks, manages organism lifecycle |
| **PeerGrain** | Peer/Connection ID | One per connected client | Tracks endpoint, version, heartbeat for peer discovery |
| **SpeciesRegistryGrain** | `"global"` | Singleton | Global catalog of registered species assemblies |
| **PopulationGrain** | Ecosystem ID | One per ecosystem | Records population snapshots for trend analysis |

## Message Flow: Ecosystem Tick

```mermaid
sequenceDiagram
    participant Timer as Tick Timer
    participant EG as EcosystemGrain
    participant POP as PopulationGrain
    participant Hub as TerrariumHub
    participant Client as Browser

    Timer->>EG: ProcessTickAsync()
    EG->>EG: Run GameEngine simulation
    EG->>POP: RecordSnapshotAsync(snapshot)
    EG->>Hub: Push EcosystemTick
    Hub->>Client: ReceiveEcosystemTick(tick)
    Hub->>Client: ReceiveWorldStateUpdate(update)
```

## Message Flow: Organism Actions

```mermaid
sequenceDiagram
    participant Client as Browser
    participant Hub as TerrariumHub
    participant EG as EcosystemGrain
    participant SR as SpeciesRegistryGrain

    Client->>Hub: TeleportCreature(teleport)
    Hub->>SR: GetAllSpeciesAsync()
    SR-->>Hub: Species list (validation)
    Hub->>EG: AddOrganismAsync(speciesId, data)
    EG-->>Hub: Confirmed
    Hub->>Client: ReceiveCreatureTeleport(teleport)
```

## Message Flow: Peer Discovery

```mermaid
sequenceDiagram
    participant Client as Browser
    participant Hub as TerrariumHub
    participant PG as PeerGrain

    Client->>Hub: AnnouncePeer(announce)
    Hub->>PG: RegisterAsync(endpoint, version)
    PG-->>Hub: OK

    loop Every 30s
        Client->>Hub: AnnouncePeer(heartbeat)
        Hub->>PG: HeartbeatAsync()
    end

    Client->>Hub: Disconnect
    Hub->>PG: Deactivate
    Hub->>Hub: Notify group: PeerAction.Leave
```

## Deployment Topology

```mermaid
graph LR
    subgraph Aspire AppHost
        direction TB
        SQL[(SQL Server)]
        Orleans[Orleans Silo<br/>In-Memory Clustering]
        Server[Terrarium.Server<br/>+ Orleans Co-hosted]
        Web[Terrarium.Web<br/>Blazor Frontend]
    end

    Web -->|HTTP + SignalR| Server
    Server --> SQL
    Server --> Orleans
```

For local development, Orleans runs co-hosted in the server process with in-memory clustering and grain storage. In production, the silo can be scaled out with Azure Table clustering and Azure Blob grain storage.

## Key Design Decisions

1. **Orleans owns all stateful domain logic** — grains are the single source of truth for world state, species, peers, and population data.
2. **SignalR is browser push channel only** — the hub delegates all logic to grains and never holds domain state.
3. **SignalR.Orleans backplane** — will be wired in when the package supports .NET 10; in-memory is used for now.
4. **Four grain types** map directly to the four bounded contexts of the original Terrarium game: ecosystem simulation, peer networking, species management, and population analytics.
