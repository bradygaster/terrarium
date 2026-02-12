# Creature Introduction Pipeline - Usage Guide

## Overview

The creature introduction pipeline enables users to upload creature assemblies to the Terrarium server and download them to introduce into their local ecosystems. This replaces the legacy peer-to-peer assembly transfer mechanism with a centralized server-based approach.

## Server Configuration

The server must be configured with an assembly storage path in `appsettings.json`:

```json
{
  "Terrarium": {
    "AssemblyPath": "C:\\TerrariumData\\Assemblies",
    // ... other settings
  }
}
```

This directory will store all uploaded creature assemblies as `.dll` files.

## API Endpoints

### 1. Register Species (Upload)

**Endpoint:** `POST /api/species/register`

**Request:**
```json
{
  "name": "MyCreature",
  "version": "1.0.0.0",
  "type": "Animal",
  "author": "John Doe",
  "email": "john@example.com",
  "assemblyFullName": "MyCreature, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
  "assemblyCode": "<base64-encoded assembly bytes>"
}
```

**Response:**
```json
{
  "status": "Success"  // or AlreadyExists, ServerDown, VersionIncompatible, etc.
}
```

### 2. Get Species Assembly (Download)

**Endpoint:** `GET /api/species/{name}/assembly?version={version}`

**Example:** `GET /api/species/MyCreature/assembly?version=1.0.0.0`

**Response:** Binary assembly data (`application/octet-stream`)

### 3. List Species

**Endpoint:** `GET /api/species/list?version={gameVersion}&filter={All|Recent}`

Returns list of all available species for download.

## Client Usage

### Introducing from Server

```csharp
// Download and introduce a creature from the server
var success = await gameEngine.IntroduceCreatureFromServerAsync(
    speciesName: "MyCreature",
    version: "1.0.0.0",
    pac: privateAssemblyCache,
    validator: assemblyValidator,
    preferredLocation: new Point(100, 100));  // optional

if (success)
{
    Console.WriteLine("Creature successfully introduced!");
}
```

### Introducing from PAC (Already Downloaded)

```csharp
// Introduce a creature that's already in the private assembly cache
var success = gameEngine.IntroduceCreatureFromPac(
    assemblyFullName: "MyCreature, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    pac: privateAssemblyCache,
    validator: assemblyValidator,
    preferredLocation: new Point(100, 100));  // optional

if (success)
{
    Console.WriteLine("Creature successfully introduced!");
}
```

## Web UI Upload

The web UI provides a file upload interface at `/upload`:

1. User selects a `.dll` file
2. File is validated using `AssemblyValidator`
3. On successful validation, assembly metadata is extracted
4. Assembly is uploaded to server via `POST /api/species/register`
5. Server saves the assembly to disk and registers in database

## Security & Validation

Both upload and download paths validate assemblies using `AssemblyValidator`:

- **Forbidden P/Invoke:** No DllImport or native methods allowed
- **Forbidden Namespaces:** System.IO, System.Net, System.Threading, etc.
- **Inheritance Chain:** Must extend `OrganismBase.Animal` or `OrganismBase.Plant`
- **File Size:** Upload limited to 10MB

If validation fails at any point, the operation is rejected and the assembly is not introduced.

## Data Flow

### Upload Flow
```
User → Web UI → Temp Storage → Validation → Server API → Disk Storage → Database
```

### Download Flow
```
Server API → Assembly Bytes → Temp File → Validation → PAC Storage → Game Engine
```

## Error Handling

All operations log errors but never throw to the caller:

- **Upload:** Returns `SpeciesServiceStatus` indicating failure reason
- **Download:** Returns `false` on failure, logs error details
- **Server errors:** Never crash game loop, gracefully degrade

## Future Enhancements

- Peer-to-peer assembly transfer (supplement, not replace server)
- Assembly caching with TTL
- Version conflict resolution
- Assembly diff/patch for updates
- Digital signatures for trusted assemblies
