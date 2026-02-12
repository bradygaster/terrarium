using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Orleans grain that maintains the global species registry.
/// Stores species metadata and assembly hashes; supports blacklisting.
/// </summary>
public class SpeciesRegistryGrain : Grain, ISpeciesRegistryGrain
{
    private readonly ILogger<SpeciesRegistryGrain> _logger;
    private readonly Dictionary<string, SpeciesEntry> _species = new(StringComparer.OrdinalIgnoreCase);

    public SpeciesRegistryGrain(ILogger<SpeciesRegistryGrain> logger)
    {
        _logger = logger;
    }

    public Task RegisterSpeciesAsync(string name, byte[] assembly)
    {
        var hash = Convert.ToHexString(SHA256.HashData(assembly));

        _species[name] = new SpeciesEntry
        {
            Name = name,
            AssemblyHash = hash,
            Assembly = assembly,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation("Species {Name} registered (hash={Hash})", name, hash);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SpeciesInfo>> GetAllSpeciesAsync()
    {
        var result = _species.Values.Select(e => new SpeciesInfo
        {
            Name = e.Name,
            AssemblyHash = e.AssemblyHash,
            RegisteredAt = e.RegisteredAt,
            IsBlacklisted = e.IsBlacklisted,
            BlacklistReason = e.BlacklistReason
        }).ToList();

        return Task.FromResult<IReadOnlyList<SpeciesInfo>>(result);
    }

    public Task BlacklistSpeciesAsync(string name, string reason)
    {
        if (_species.TryGetValue(name, out var entry))
        {
            entry.IsBlacklisted = true;
            entry.BlacklistReason = reason;
            _logger.LogWarning("Species {Name} blacklisted: {Reason}", name, reason);
        }
        else
        {
            _logger.LogWarning("Attempted to blacklist unknown species {Name}", name);
        }
        return Task.CompletedTask;
    }

    private sealed class SpeciesEntry
    {
        public required string Name { get; init; }
        public required string AssemblyHash { get; init; }
        public required byte[] Assembly { get; init; }
        public required DateTimeOffset RegisteredAt { get; init; }
        public bool IsBlacklisted { get; set; }
        public string? BlacklistReason { get; set; }
    }
}
