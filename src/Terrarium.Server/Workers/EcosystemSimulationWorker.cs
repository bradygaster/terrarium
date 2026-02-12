using Microsoft.AspNetCore.SignalR;
using Terrarium.Net;

namespace Terrarium.Server.Workers;

/// <summary>
/// Background service that runs a simple ecosystem simulation and broadcasts
/// world state to all connected SignalR clients every tick (500ms).
/// Seeds plants, herbivores, and carnivores on startup, then runs movement,
/// feeding, energy, death, and reproduction logic each tick.
/// </summary>
public sealed class EcosystemSimulationWorker : BackgroundService
{
    private readonly IHubContext<TerrariumHub, ITerrariumClient> _hubContext;
    private readonly ILogger<EcosystemSimulationWorker> _logger;

    private const string EcosystemId = "default";
    private const int WorldWidth = 5000;
    private const int WorldHeight = 5000;
    private const int TickIntervalMs = 500;

    // Simulation tuning
    private const float HerbivoreSpeed = 8f;
    private const float CarnivoreSpeed = 14f;
    private const float EatRange = 40f;
    private const int ReproduceEnergyThreshold = 80;
    private const int MaxOrganisms = 5000;

    private readonly List<Organism> _organisms = [];
    private long _tickNumber;

    private static readonly string[] HerbivoreNames =
        ["Leafy", "Sprout", "Clover", "Fern", "Moss", "Ivy", "Daisy", "Basil", "Sage", "Thyme"];
    private static readonly string[] CarnivoreNames =
        ["Fang", "Claw", "Shadow", "Razor", "Blaze", "Storm", "Viper", "Talon", "Dusk", "Onyx"];
    private static readonly string[] PlantNames =
        ["Grass", "Shrub", "Flower", "Reed", "Kelp", "Bloom", "Petal", "Thorn", "Root", "Seed"];
    private static readonly string[] SkinFamilies =
        ["ant", "beetle", "inchworm", "spider", "scorpion"];

    public EcosystemSimulationWorker(
        IHubContext<TerrariumHub, ITerrariumClient> hubContext,
        ILogger<EcosystemSimulationWorker> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EcosystemSimulationWorker starting — seeding world");

        SeedWorld();

        _logger.LogInformation(
            "World seeded: {Count} organisms ({Plants} plants, {Herbs} herbivores, {Carns} carnivores)",
            _organisms.Count,
            _organisms.Count(o => o.Type == OrganismType.Plant),
            _organisms.Count(o => o.Type == OrganismType.Herbivore),
            _organisms.Count(o => o.Type == OrganismType.Carnivore));

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _tickNumber++;
                SimulateTick();
                await BroadcastAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EcosystemSimulationWorker tick {Tick} failed", _tickNumber);
            }
        }

        _logger.LogInformation("EcosystemSimulationWorker stopped at tick {Tick}", _tickNumber);
    }

    private void SeedWorld()
    {
        var rng = Random.Shared;

        // Plants: 2000-3000
        var plantCount = rng.Next(2000, 3001);
        for (var i = 0; i < plantCount; i++)
        {
            _organisms.Add(new Organism
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = PlantNames[rng.Next(PlantNames.Length)] + rng.Next(1000),
                Species = "Plant",
                Type = OrganismType.Plant,
                X = rng.Next(WorldWidth),
                Y = rng.Next(WorldHeight),
                Energy = rng.Next(40, 100),
                SkinFamily = "plant"
            });
        }

        // Herbivores: 200-300
        var herbCount = rng.Next(200, 301);
        for (var i = 0; i < herbCount; i++)
        {
            _organisms.Add(new Organism
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = HerbivoreNames[rng.Next(HerbivoreNames.Length)] + rng.Next(1000),
                Species = "Herbivore",
                Type = OrganismType.Herbivore,
                X = rng.Next(WorldWidth),
                Y = rng.Next(WorldHeight),
                Energy = rng.Next(50, 100),
                SkinFamily = SkinFamilies[rng.Next(SkinFamilies.Length)],
                DirectionX = (float)(rng.NextDouble() * 2 - 1),
                DirectionY = (float)(rng.NextDouble() * 2 - 1)
            });
        }

        // Carnivores: 30-50
        var carnCount = rng.Next(30, 51);
        for (var i = 0; i < carnCount; i++)
        {
            _organisms.Add(new Organism
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = CarnivoreNames[rng.Next(CarnivoreNames.Length)] + rng.Next(1000),
                Species = "Carnivore",
                Type = OrganismType.Carnivore,
                X = rng.Next(WorldWidth),
                Y = rng.Next(WorldHeight),
                Energy = rng.Next(60, 100),
                SkinFamily = SkinFamilies[rng.Next(SkinFamilies.Length)],
                DirectionX = (float)(rng.NextDouble() * 2 - 1),
                DirectionY = (float)(rng.NextDouble() * 2 - 1)
            });
        }
    }

    private void SimulateTick()
    {
        var rng = Random.Shared;
        var newOrganisms = new List<Organism>();

        for (var i = _organisms.Count - 1; i >= 0; i--)
        {
            var org = _organisms[i];

            switch (org.Type)
            {
                case OrganismType.Plant:
                    SimulatePlant(org, newOrganisms, rng);
                    break;
                case OrganismType.Herbivore:
                    SimulateHerbivore(org, rng);
                    break;
                case OrganismType.Carnivore:
                    SimulateCarnivore(org, rng);
                    break;
            }

            // Death check
            if (org.Energy <= 0)
            {
                _organisms.RemoveAt(i);
            }
        }

        // Herbivore reproduction
        for (var i = _organisms.Count - 1; i >= 0; i--)
        {
            var org = _organisms[i];
            if (org.Type == OrganismType.Herbivore && org.Energy > ReproduceEnergyThreshold && rng.NextDouble() < 0.02)
            {
                TryReproduce(org, newOrganisms, rng);
            }
            else if (org.Type == OrganismType.Carnivore && org.Energy > ReproduceEnergyThreshold && rng.NextDouble() < 0.01)
            {
                TryReproduce(org, newOrganisms, rng);
            }
        }

        // Add new organisms (cap total population)
        if (_organisms.Count + newOrganisms.Count <= MaxOrganisms)
        {
            _organisms.AddRange(newOrganisms);
        }
        else
        {
            var remaining = MaxOrganisms - _organisms.Count;
            if (remaining > 0)
            {
                _organisms.AddRange(newOrganisms.Take(remaining));
            }
        }
    }

    private static void SimulatePlant(Organism plant, List<Organism> newOrganisms, Random rng)
    {
        // Regenerate energy slowly
        if (plant.Energy < 100)
        {
            plant.Energy = Math.Min(100, plant.Energy + 1);
        }

        // Occasionally reproduce (spawn new plant nearby)
        if (plant.Energy > 60 && rng.NextDouble() < 0.001)
        {
            var offsetX = rng.Next(-100, 101);
            var offsetY = rng.Next(-100, 101);
            newOrganisms.Add(new Organism
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = PlantNames[rng.Next(PlantNames.Length)] + rng.Next(1000),
                Species = "Plant",
                Type = OrganismType.Plant,
                X = Math.Clamp(plant.X + offsetX, 0, WorldWidth - 1),
                Y = Math.Clamp(plant.Y + offsetY, 0, WorldHeight - 1),
                Energy = 30,
                SkinFamily = "plant"
            });
        }
    }

    private void SimulateHerbivore(Organism herb, Random rng)
    {
        // Random direction change
        if (rng.NextDouble() < 0.1)
        {
            herb.DirectionX = (float)(rng.NextDouble() * 2 - 1);
            herb.DirectionY = (float)(rng.NextDouble() * 2 - 1);
        }

        // Move
        herb.X = Math.Clamp(herb.X + herb.DirectionX * HerbivoreSpeed, 0, WorldWidth - 1);
        herb.Y = Math.Clamp(herb.Y + herb.DirectionY * HerbivoreSpeed, 0, WorldHeight - 1);

        // Bounce off edges
        if (herb.X <= 0 || herb.X >= WorldWidth - 1) herb.DirectionX = -herb.DirectionX;
        if (herb.Y <= 0 || herb.Y >= WorldHeight - 1) herb.DirectionY = -herb.DirectionY;

        // Lose energy
        herb.Energy -= 1;

        // Try to eat a nearby plant
        for (var i = 0; i < _organisms.Count; i++)
        {
            var target = _organisms[i];
            if (target.Type != OrganismType.Plant || target.Energy <= 0) continue;

            var dx = herb.X - target.X;
            var dy = herb.Y - target.Y;
            if (dx * dx + dy * dy < EatRange * EatRange)
            {
                var gained = Math.Min(target.Energy, 20);
                herb.Energy = Math.Min(100, herb.Energy + gained);
                target.Energy -= gained;
                break;
            }
        }
    }

    private void SimulateCarnivore(Organism carn, Random rng)
    {
        // Random direction change
        if (rng.NextDouble() < 0.08)
        {
            carn.DirectionX = (float)(rng.NextDouble() * 2 - 1);
            carn.DirectionY = (float)(rng.NextDouble() * 2 - 1);
        }

        // Move (faster than herbivores)
        carn.X = Math.Clamp(carn.X + carn.DirectionX * CarnivoreSpeed, 0, WorldWidth - 1);
        carn.Y = Math.Clamp(carn.Y + carn.DirectionY * CarnivoreSpeed, 0, WorldHeight - 1);

        // Bounce off edges
        if (carn.X <= 0 || carn.X >= WorldWidth - 1) carn.DirectionX = -carn.DirectionX;
        if (carn.Y <= 0 || carn.Y >= WorldHeight - 1) carn.DirectionY = -carn.DirectionY;

        // Lose energy (slightly faster than herbivores)
        carn.Energy -= 1;

        // Try to eat a nearby herbivore
        for (var i = 0; i < _organisms.Count; i++)
        {
            var target = _organisms[i];
            if (target.Type != OrganismType.Herbivore || target.Energy <= 0) continue;

            var dx = carn.X - target.X;
            var dy = carn.Y - target.Y;
            if (dx * dx + dy * dy < EatRange * EatRange)
            {
                var gained = Math.Min(target.Energy, 30);
                carn.Energy = Math.Min(100, carn.Energy + gained);
                target.Energy = 0; // Kill herbivore
                break;
            }
        }
    }

    private static void TryReproduce(Organism parent, List<Organism> newOrganisms, Random rng)
    {
        parent.Energy -= 30;

        var names = parent.Type == OrganismType.Herbivore ? HerbivoreNames : CarnivoreNames;
        newOrganisms.Add(new Organism
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = names[rng.Next(names.Length)] + rng.Next(1000),
            Species = parent.Species,
            Type = parent.Type,
            X = Math.Clamp(parent.X + rng.Next(-50, 51), 0, WorldWidth - 1),
            Y = Math.Clamp(parent.Y + rng.Next(-50, 51), 0, WorldHeight - 1),
            Energy = 40,
            SkinFamily = parent.SkinFamily,
            DirectionX = (float)(rng.NextDouble() * 2 - 1),
            DirectionY = (float)(rng.NextDouble() * 2 - 1)
        });
    }

    private async Task BroadcastAsync()
    {
        var creatures = new List<CreatureStateData>(_organisms.Count);
        foreach (var org in _organisms)
        {
            creatures.Add(new CreatureStateData
            {
                Id = org.Id,
                Name = org.Name,
                Species = org.Species,
                SkinFamily = org.SkinFamily,
                X = org.X,
                Y = org.Y,
                Energy = org.Energy
            });
        }

        var tick = new EcosystemTick
        {
            EcosystemId = EcosystemId,
            TickNumber = _tickNumber,
            TickDurationMs = TickIntervalMs,
            PeerCount = TerrariumHub.GetConnectedPeerCount(),
            OrganismCount = _organisms.Count
        };

        var worldState = new WorldStateUpdate
        {
            EcosystemId = EcosystemId,
            TickNumber = _tickNumber,
            WorldWidth = WorldWidth,
            WorldHeight = WorldHeight,
            OrganismCount = _organisms.Count,
            Creatures = creatures
        };

        await _hubContext.Clients.All.ReceiveEcosystemTick(tick);
        await _hubContext.Clients.All.ReceiveWorldStateUpdate(worldState);

        if (_tickNumber % 100 == 0)
        {
            _logger.LogInformation(
                "Tick {Tick}: {Total} organisms (P:{Plants} H:{Herbs} C:{Carns})",
                _tickNumber,
                _organisms.Count,
                _organisms.Count(o => o.Type == OrganismType.Plant),
                _organisms.Count(o => o.Type == OrganismType.Herbivore),
                _organisms.Count(o => o.Type == OrganismType.Carnivore));
        }
    }

    private enum OrganismType { Plant, Herbivore, Carnivore }

    private sealed class Organism
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Species { get; set; }
        public required OrganismType Type { get; init; }
        public float X { get; set; }
        public float Y { get; set; }
        public int Energy { get; set; }
        public required string SkinFamily { get; set; }
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
    }
}
