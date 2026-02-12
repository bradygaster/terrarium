using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Manages a single ecosystem instance — world state, tick processing, and organism lifecycle.
/// Keyed by ecosystem ID (string).
/// </summary>
public interface IEcosystemGrain : IGrainWithStringKey
{
    Task<WorldSnapshot> GetWorldStateAsync();
    Task ProcessTickAsync();
    Task AddOrganismAsync(string speciesId, byte[] assemblyData);
    Task RemoveOrganismAsync(string organismId);
    Task<int> GetCreatureCountAsync();
}
