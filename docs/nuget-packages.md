# NuGet Packages for Terrarium

This document describes the NuGet packages for the Terrarium project.

## Packages

### Terrarium.OrganismBase
**Package ID**: `Terrarium.OrganismBase`  
**Version**: `10.0.0-preview.1`  
**Target Framework**: net10.0

Base library for creating creatures in the .NET Terrarium ecosystem simulation. Contains:
- Core types: `Animal`, `Plant`, `OrganismState`
- Characteristic attributes: `CarnivoreAttribute`, `MatureSizeAttribute`, etc.
- Engine interfaces and event models

**Installation**:
```bash
dotnet add package Terrarium.OrganismBase
```

### Terrarium.Templates
**Package ID**: `Terrarium.Templates`  
**Version**: `10.0.0-preview.1`  
**Package Type**: Template

dotnet new templates for scaffolding creature projects.

**Installation**:
```bash
dotnet new install Terrarium.Templates
```

**Usage**:
```bash
# Create an herbivore (default)
dotnet new terrarium-creature -n MyHerbivore

# Create a carnivore
dotnet new terrarium-creature -n MyCarnivore --IsCarnivore true

# Create a plant
dotnet new terrarium-creature -n MyPlant --CreatureType Plant

# With custom author info
dotnet new terrarium-creature -n MyCreature \
  --AuthorName "Your Name" \
  --AuthorEmail "you@example.com"
```

## Publishing

### Manual Publishing

Build and pack the packages:
```bash
dotnet pack src/Terrarium.OrganismBase/Terrarium.OrganismBase.csproj --configuration Release --output ./packages
dotnet pack src/Terrarium.Templates/Terrarium.Templates.csproj --configuration Release --output ./packages
```

Publish to GitHub Packages:
```bash
dotnet nuget push "./packages/*.nupkg" \
  --source "https://nuget.pkg.github.com/OWNER/index.json" \
  --api-key YOUR_GITHUB_TOKEN
```

### Automated Publishing via GitHub Actions

The workflow `.github/workflows/nuget-publish.yml` automates package publishing.

**Trigger options**:

1. **Tag-based release** (recommended):
   ```bash
   git tag v10.0.0-preview.1
   git push origin v10.0.0-preview.1
   ```

2. **Manual dispatch** via GitHub UI:
   - Go to Actions → Publish NuGet Packages → Run workflow
   - Enter the desired package version

The workflow:
- Builds the solution
- Packs both OrganismBase and Templates
- Publishes to GitHub Packages
- Uploads packages as artifacts (30-day retention)

## Package Contents

### OrganismBase Package Structure
```
Terrarium.OrganismBase.10.0.0-preview.1.nupkg
├── lib/net10.0/
│   ├── OrganismBase.dll
│   └── OrganismBase.xml (API documentation)
└── README.md
```

Symbol package (`.snupkg`) is also generated for debugging support.

### Templates Package Structure
```
Terrarium.Templates.10.0.0-preview.1.nupkg
└── content/
    └── terrarium-creature/
        ├── .template.config/
        │   └── template.json
        ├── TerrariumCreature.cs
        ├── TerrariumCreature.csproj
        └── README.md
```

## Template Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `--CreatureType` | choice | Animal or Plant | Animal |
| `--IsCarnivore` | choice | true (carnivore) or false (herbivore) | false |
| `--AuthorName` | string | Creature author name | Anonymous |
| `--AuthorEmail` | string | Creature author email | author@example.com |
| `--Framework` | choice | Target framework | net10.0 |

## Version Strategy

Packages follow the Terrarium version numbering:
- **Major.Minor**: Matches .NET version (10.0 for .NET 10)
- **Patch**: Incremental updates
- **Suffix**: `-preview.X` for preview releases, none for stable

Example progression:
- `10.0.0-preview.1` (initial preview)
- `10.0.0-preview.2` (preview update)
- `10.0.0` (stable release)
- `10.0.1` (patch release)

## Consuming the Packages

### For Creature Developers

1. Install templates:
   ```bash
   dotnet new install Terrarium.Templates
   ```

2. Create a new creature:
   ```bash
   dotnet new terrarium-creature -n MyAwesomeCreature
   ```

3. Customize and build:
   ```bash
   cd MyAwesomeCreature
   # Edit MyAwesomeCreature.cs to implement behavior
   dotnet build
   ```

4. Deploy the compiled DLL to your Terrarium client.

### For Terrarium Development

Reference OrganismBase directly:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Terrarium.OrganismBase\Terrarium.OrganismBase.csproj" />
</ItemGroup>
```

## Troubleshooting

### Template installation issues
```bash
# List installed templates
dotnet new list

# Uninstall if needed
dotnet new uninstall Terrarium.Templates

# Reinstall
dotnet new install Terrarium.Templates
```

### Package restore issues
Ensure GitHub Packages is configured in your NuGet sources:
```bash
dotnet nuget add source "https://nuget.pkg.github.com/OWNER/index.json" \
  --name "github" \
  --username "YOUR_USERNAME" \
  --password "YOUR_GITHUB_TOKEN" \
  --store-password-in-clear-text
```

## License

Both packages are licensed under MIT. See the root LICENSE file for details.
