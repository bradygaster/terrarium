using System.IO;
using OrganismBase;

// Assembly-level attributes identify this creature to the Terrarium engine
[assembly: OrganismClass("Terrarium.Samples.SimplePlant.SimplePlant")]
[assembly: AuthorInformation("Terrarium Team", "terrarium@example.com")]

namespace Terrarium.Samples.SimplePlant;

/// <summary>
/// A basic plant that reproduces automatically.
/// Demonstrates: the simplest possible organism — just exists, grows, and spreads.
/// Plants don't need event handlers; the base class handles reproduction automatically.
/// </summary>
[MatureSize(24)]
[PlantSkin(PlantSkinFamily.Plant)]
[SeedSpreadDistance(0)]
[MarkingColor(System.Drawing.KnownColor.Green)]
public class SimplePlant : Plant
{
    public override void SerializePlant(MemoryStream m) { }
    public override void DeserializePlant(MemoryStream m) { }
}
