### 2025-01-23: Ecosystem Mode Selection and Game State Persistence

**By:** Mike (Engine/Networking)

**What:** Implemented two major features for Sprint 12:
1. **Issue #81 - Ecosystem mode selection**: Created `EcosystemMode` enum with LocalOnly and Networked modes. Updated GameEngine to check mode before network operations (teleportation, population reporting). Mode can be switched at startup and runtime via `GameEngine.Mode` property.
2. **Issue #82 - Save/Load game state**: Created `IGameStatePersistence` interface and `GameStatePersistence` implementation using System.Text.Json. Serialization includes organism positions, species data, energy levels, and tick count. Added `SaveGameStateAsync` and `LoadGameStateAsync` methods to GameEngine.

**Why:** 
- LocalOnly mode enables offline gameplay and testing without requiring server infrastructure
- Networked mode preserves existing multiplayer functionality
- Save/Load enables game session persistence and recovery
- Using System.Text.Json provides modern, performant serialization
- Interface-based design allows multiple persistence backends (server, browser download, local file)

**Implementation Details:**
- `EcosystemMode.cs`: Simple enum with LocalOnly (0) and Networked (1) values
- Updated `GameEngine.TeleportOrganisms()` to skip teleportation in LocalOnly mode
- Updated `PopulationData.EndTick()` to skip server reporting in LocalOnly mode
- Mode changes propagate from GameEngine to PopulationData automatically
- `IGameStatePersistence` defines serialize/deserialize and save/load contracts
- `GameStatePersistence` implements JSON serialization with WorldStateSaveData and OrganismSaveData DTOs
- Registered `IGameStatePersistence` in DI container via GameServiceExtensions
- Save/Load methods integrate with existing GameEngine architecture

**Organism Assembly Handling:**
Save operations store assembly full names and species references. Load operations require the caller to provide a PrivateAssemblyCache (PAC) to reload creature assemblies. This design separates serialization concerns from assembly loading/validation.

**Future Work:**
- Server-side save/load endpoints (SaveToServerAsync, LoadFromServerAsync) - marked as TODO
- Browser download trigger (SaveAsBrowserDownloadAsync) - requires JSInterop in Blazor context
- Organism restoration from save data - requires PAC integration in LoadGameStateAsync
- Settings UI integration for mode selection
- Runtime mode switching with proper network connection teardown
