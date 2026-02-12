// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Game.Services;

/// <summary>
/// Bridges the GameEngine to server HTTP services via the Terrarium.Services layer.
/// Handles population reporting (every 600 ticks), species registration,
/// and error reporting (Watson-style crash data).
/// </summary>
public sealed class GameServiceBridge
{
    private readonly IPopulationService _populationService;
    private readonly ISpeciesService _speciesService;
    private readonly IReportingService _reportingService;
    private readonly IWatsonService _watsonService;
    private readonly ILogger<GameServiceBridge> _logger;
    private readonly Guid _peerGuid;

    public GameServiceBridge(
        IPopulationService populationService,
        ISpeciesService speciesService,
        IReportingService reportingService,
        IWatsonService watsonService,
        ILogger<GameServiceBridge> logger,
        Guid? peerGuid = null)
    {
        _populationService = populationService;
        _speciesService = speciesService;
        _reportingService = reportingService;
        _watsonService = watsonService;
        _logger = logger;
        _peerGuid = peerGuid ?? Guid.NewGuid();
    }

    /// <summary>
    /// Reports population data to the server. Called every 600 ticks by PopulationData.
    /// </summary>
    public async Task ReportPopulationAsync(
        int tickNumber,
        IReadOnlyDictionary<string, int> speciesPopulations,
        CancellationToken cancellationToken = default)
    {
        var history = new List<PopulationHistoryRow>();
        foreach (var (speciesName, population) in speciesPopulations)
        {
            history.Add(new PopulationHistoryRow
            {
                Guid = _peerGuid,
                TickNumber = tickNumber,
                SpeciesName = speciesName,
                ClientTime = DateTime.UtcNow,
                CorrectTime = 0,
                Population = population
            });
        }

        try
        {
            var result = await _populationService.ReportPopulationAsync(
                _peerGuid, tickNumber, history, cancellationToken);

            _logger.LogInformation(
                "Population reported at tick {Tick}: {Count} species, result={Result}",
                tickNumber, history.Count, result.ReturnCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report population at tick {Tick}", tickNumber);
        }
    }

    /// <summary>
    /// Registers a new species with the server when a creature is first introduced.
    /// </summary>
    public async Task RegisterSpeciesAsync(
        Species species,
        byte[]? assemblyBytes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _speciesService.AddAsync(
                name: species.Name,
                version: "1.0", // TODO: read from game config
                type: species is AnimalSpecies ? "Animal" : "Plant",
                author: species.AuthorName,
                email: species.AuthorEmail,
                assemblyFullName: species.AssemblyFullName,
                assemblyCode: assemblyBytes ?? Array.Empty<byte>(),
                cancellationToken);

            _logger.LogInformation("Species registered: {Name}, status={Status}",
                species.Name, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register species {Name}", species.Name);
        }
    }

    /// <summary>
    /// Downloads a species assembly from the server by name and version.
    /// </summary>
    public async Task<byte[]?> GetSpeciesAssemblyAsync(
        string speciesName,
        string version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assemblyBytes = await _speciesService.GetSpeciesAssemblyAsync(speciesName, version, cancellationToken);
            _logger.LogInformation("Downloaded species assembly: {Name} ({Size} bytes)", speciesName, assemblyBytes.Length);
            return assemblyBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download species assembly: {Name}", speciesName);
            return null;
        }
    }

    /// <summary>
    /// Reports an error to the server (Watson-style crash data).
    /// </summary>
    public async Task ReportErrorAsync(
        string errorLog,
        string? logType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = new WatsonReport
            {
                LogType = logType ?? "GameEngine",
                ErrorLog = errorLog,
                GameVersion = "10.0",
                CLRVersion = RuntimeInformation.FrameworkDescription,
                OSVersion = RuntimeInformation.OSDescription
            };

            var success = await _watsonService.ReportErrorAsync(report, cancellationToken);
            _logger.LogInformation("Error reported to Watson: success={Success}", success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report error to Watson");
        }
    }

    /// <summary>
    /// Reports a bug to the reporting service.
    /// </summary>
    public async Task ReportBugAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = new BugReport
            {
                Title = title,
                Description = description,
                Version = "10.0"
            };

            await _reportingService.ReportBugAsync(report, cancellationToken);
            _logger.LogInformation("Bug reported: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report bug: {Title}", title);
        }
    }
}
