using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface ISpeciesService
{
    Task<SpeciesServiceStatus> AddAsync(string name, string version, string type, string author,
        string email, string assemblyFullName, byte[] assemblyCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesInfo>> GetExtinctSpeciesAsync(string version, string filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesInfo>> GetAllSpeciesAsync(string version, string filter,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetSpeciesAssemblyAsync(string name, string version,
        CancellationToken cancellationToken = default);

    Task<byte[]> ReintroduceSpeciesAsync(string name, string version, Guid peerGuid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetBlacklistedSpeciesAsync(CancellationToken cancellationToken = default);
}
