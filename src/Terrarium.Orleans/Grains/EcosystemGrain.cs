using Microsoft.Extensions.Logging;
using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Orleans grain that owns the stateful world for a single ecosystem.
/// Delegates tick processing to the game engine and notifies connected
/// clients via SignalR after each state change.
/// </summary>
public class EcosystemGrain : Grain, IEcosystemGrain
{
    private readonly ILogger<EcosystemGrain> _logger;

    private long _tickNumber;
    private int _worldWidth = 100;
    private int _worldHeight = 100;
    private readonly Dictionary<string, byte[]> _organisms = new();

    public EcosystemGrain(ILogger<EcosystemGrain> logger)
    {
        _logger = logger;
    }

    public Task<WorldSnapshot> GetWorldStateAsync()
    {
        var snapshot = new WorldSnapshot
        {
            EcosystemId = this.GetPrimaryKeyString(),
            TickNumber = _tickNumber,
            WorldWidth = _worldWidth,
            WorldHeight = _worldHeight,
            OrganismCount = _organisms.Count
        };
        return Task.FromResult(snapshot);
    }

    public Task ProcessTickAsync()
    {
        _tickNumber++;
        _logger.LogDebug("Ecosystem {Id} processed tick {Tick} with {Count} organisms",
            this.GetPrimaryKeyString(), _tickNumber, _organisms.Count);

        // TODO: Delegate to GameEngine for actual simulation logic
        // TODO: Push WorldStateUpdate to SignalR clients via IHubContext

        return Task.CompletedTask;
    }

    public Task AddOrganismAsync(string speciesId, byte[] assemblyData)
    {
        var organismId = $"{speciesId}_{Guid.NewGuid():N}";
        _organisms[organismId] = assemblyData;
        _logger.LogInformation("Added organism {OrganismId} (species {SpeciesId}) to ecosystem {Id}",
            organismId, speciesId, this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    public Task RemoveOrganismAsync(string organismId)
    {
        if (_organisms.Remove(organismId))
        {
            _logger.LogInformation("Removed organism {OrganismId} from ecosystem {Id}",
                organismId, this.GetPrimaryKeyString());
        }
        return Task.CompletedTask;
    }

    public Task<int> GetCreatureCountAsync()
    {
        return Task.FromResult(_organisms.Count);
    }
}
