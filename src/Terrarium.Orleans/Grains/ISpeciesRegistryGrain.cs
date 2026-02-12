using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Global species catalog — registers assemblies, tracks blacklists.
/// Singleton grain keyed by a well-known key (e.g., "global").
/// </summary>
public interface ISpeciesRegistryGrain : IGrainWithStringKey
{
    Task RegisterSpeciesAsync(string name, byte[] assembly);
    Task<IReadOnlyList<SpeciesInfo>> GetAllSpeciesAsync();
    Task BlacklistSpeciesAsync(string name, string reason);
}
