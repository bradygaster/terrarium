// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.Extensions.Logging;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Tracks population metrics. Replaces legacy DataSet/WebService with ILogger.
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

    public PopulationData(bool reportData, ILogger<PopulationData> logger)
    { _reportDataToServer = reportData; _logger = logger; }

    public int LastReportedTick => _lastReportedTick;
    public bool IsReportingTick(int tickNumber) => tickNumber > 0 && tickNumber % TicksToReport == 0;

    public void BeginTick(int tickNumber, Guid stateGuid)
    { _currentTick = tickNumber; _currentStateGuid = stateGuid; }

    public void EndTick(int tickNumber)
    {
        if (IsReportingTick(tickNumber))
        {
            _lastReportedTick = tickNumber;
            if (_reportDataToServer)
            {
                // TODO: Sprint 7 - report population data via Orleans grain or HTTP
                _logger.LogInformation("Population report at tick {Tick}: {Count} species tracked",
                    tickNumber, _speciesPopulations.Count);
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
