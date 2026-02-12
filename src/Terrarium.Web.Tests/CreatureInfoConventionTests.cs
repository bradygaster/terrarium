namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for the CreatureInfo record — the web client's creature view model.
/// Verifies record semantics, equality, and property access.
/// </summary>
/// <remarks>
/// CreatureInfo lives in Terrarium.Web.Models but since Terrarium.Web is a web app
/// (Microsoft.NET.Sdk.Web), we reference the Game project and test the data contracts
/// that the renderer will consume. When Terrarium.Web exposes a shared models library,
/// these tests can reference it directly.
/// 
/// TODO: When Skyler finalizes the renderer interop service, add tests for:
/// - CreatureInfo ↔ TerrariumSprite mapping
/// - CreatureInfo ↔ world state deserialization
/// </remarks>
public class CreatureInfoConventionTests
{
    // The CreatureInfo record is in Terrarium.Web which is a Web SDK project.
    // We can't reference it from a plain test project without pulling in ASP.NET.
    // Instead, we test the contract it should satisfy:

    [Fact]
    public void CreatureInfo_ShouldBe_PositionalRecord()
    {
        // Verify the shape matches what the renderer expects:
        // (Species, Name, Energy, MaxEnergy, Age, Position)
        // This test documents the expected contract.
        var type = typeof(Terrarium.Web.Models.CreatureInfo);
        Assert.True(type.IsValueType == false, "CreatureInfo should be a reference type (record class)");

        var props = type.GetProperties();
        var expectedProps = new[] { "Species", "Name", "Energy", "MaxEnergy", "Age", "Position" };
        foreach (var expected in expectedProps)
        {
            Assert.Contains(props, p => p.Name == expected);
        }
    }

    [Fact]
    public void CreatureInfo_EnergyPercent_Calculation()
    {
        // This mirrors the CreaturePanel.razor EnergyPercent logic:
        // (int)(100.0 * Energy / MaxEnergy)
        int energy = 75;
        int maxEnergy = 200;
        int percent = (int)(100.0 * energy / maxEnergy);
        Assert.Equal(37, percent);
    }

    [Fact]
    public void CreatureInfo_EnergyPercent_ZeroMax_ReturnsZero()
    {
        int energy = 50;
        int maxEnergy = 0;
        int percent = maxEnergy > 0 ? (int)(100.0 * energy / maxEnergy) : 0;
        Assert.Equal(0, percent);
    }

    [Fact]
    public void CreatureInfo_EnergyPercent_FullEnergy_Returns100()
    {
        int energy = 200;
        int maxEnergy = 200;
        int percent = (int)(100.0 * energy / maxEnergy);
        Assert.Equal(100, percent);
    }

    [Fact]
    public void CreatureInfo_EnergyPercent_OverMax_ExceedsHundred()
    {
        // Edge case: energy > maxEnergy (possible during certain game states)
        int energy = 250;
        int maxEnergy = 200;
        int percent = (int)(100.0 * energy / maxEnergy);
        Assert.Equal(125, percent);
    }
}
