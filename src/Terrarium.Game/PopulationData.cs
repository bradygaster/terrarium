// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.Extensions.Logging;
using OrganismBase;
using Terrarium.Game.Services;

namespace Terrarium.Game;

/// <summary>
/// Tracks population metrics. Replaces legacy DataSet/WebService with ILogger.
/// When a GameServiceBridge is attached, reports population to the server every 600 ticks.
/// </summary>
public sealed class PopulationData
{
    private const int TicksToReport = 600;
    private readonly ILogger<PopulationData> _logger;
    private readonly bool _reportDataToServer;
    private readonly Dictionary<string, int> _speciesPopulations = new();
    private int _currentTick = -1;
    private int _lastReportedTick = -1;
    private Guid _currentStateGuid;
    private GameServiceBridge? _serviceBridge;
    private EcosystemMode _mode = EcosystemMode.LocalOnly;

    public PopulationData(bool reportData, ILogger<PopulationData> logger)
    { _reportDataToServer = reportData; _logger = logger; }

    public int LastReportedTick => _lastReportedTick;
    public bool IsReportingTick(int tickNumber) => tickNumber > 0 && tickNumber % TicksToReport == 0;

    /// <summary>Sets the service bridge for HTTP-based population reporting.</summary>
    public GameServiceBridge? ServiceBridge
    {
        get => _serviceBridge;
        set => _serviceBridge = value;
    }

    /// <summary>Gets or sets the ecosystem mode (affects population reporting).</summary>
    public EcosystemMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public void BeginTick(int tickNumber, Guid stateGuid)
    { _currentTick = tickNumber; _currentStateGuid = stateGuid; }

    public void EndTick(int tickNumber)
    {
        if (IsReportingTick(tickNumber))
        {
            _lastReportedTick = tickNumber;
            // Skip population reporting in local-only mode
            if (_reportDataToServer && _mode == EcosystemMode.Networked)
            {
                _logger.LogInformation("Population report at tick {Tick}: {Count} species tracked",
                    tickNumber, _speciesPopulations.Count);

                if (_serviceBridge is not null)
                {
                    var snapshot = GetPopulationSnapshot();
                    // Fire-and-forget: reporting should not block the game loop
                    _ = _serviceBridge.ReportPopulationAsync(tickNumber, snapshot);
                }
            }
        }
    }

    public void CountOrganism(string speciesName, PopulationChangeReason reason)
    {
        _speciesPopulations.TryGetValue(speciesName, out var count);
        _speciesPopulations[speciesName] = count + 1;
    }

    public void UncountOrganism(string speciesName, PopulationChangeReason reason)
    {
        if (_speciesPopulations.TryGetValue(speciesName, out var count))
            _speciesPopulations[speciesName] = Math.Max(0, count - 1);
    }

    public IReadOnlyDictionary<string, int> GetPopulationSnapshot()
        => new Dictionary<string, int>(_speciesPopulations);

    public void Close() { }
}
