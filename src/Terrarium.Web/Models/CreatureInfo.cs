namespace Terrarium.Web.Models;

/// <summary>
/// Represents a creature in the Terrarium ecosystem.
/// </summary>
public sealed record CreatureInfo(
    string Species,
    string Name,
    int Energy,
    int MaxEnergy,
    int Age,
    string Position);
