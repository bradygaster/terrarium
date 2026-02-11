using System.Drawing;
using OrganismBase;
using Xunit;

namespace Terrarium.OrganismBase.Tests;

public class NonPointAttributeTests
{
    // --- CarnivoreAttribute ---

    [Fact]
    public void CarnivoreAttribute_True_SetsIsCarnivore()
    {
        var attr = new CarnivoreAttribute(true);
        Assert.True(attr.IsCarnivore);
    }

    [Fact]
    public void CarnivoreAttribute_False_SetsHerbivore()
    {
        var attr = new CarnivoreAttribute(false);
        Assert.False(attr.IsCarnivore);
    }

    // --- MatureSizeAttribute ---

    [Fact]
    public void MatureSize_ValidSize_SetsMatureRadius()
    {
        var attr = new MatureSizeAttribute(EngineSettings.MinMatureSize);
        Assert.Equal(EngineSettings.MinMatureSize / 2, attr.MatureRadius);
    }

    [Fact]
    public void MatureSize_MaxSize_SetsMatureRadius()
    {
        var attr = new MatureSizeAttribute(EngineSettings.MaxMatureSize);
        Assert.Equal(EngineSettings.MaxMatureSize / 2, attr.MatureRadius);
    }

    [Fact]
    public void MatureSize_TooSmall_Throws()
    {
        Assert.Throws<SizeOutOfRangeCharacteristicException>(() =>
            new MatureSizeAttribute(EngineSettings.MinMatureSize - 1));
    }

    [Fact]
    public void MatureSize_TooLarge_Throws()
    {
        Assert.Throws<SizeOutOfRangeCharacteristicException>(() =>
            new MatureSizeAttribute(EngineSettings.MaxMatureSize + 1));
    }

    // --- SeedSpreadDistanceAttribute ---

    [Fact]
    public void SeedSpread_ValidDistance_SetsValue()
    {
        var attr = new SeedSpreadDistanceAttribute(100);
        Assert.Equal(100, attr.SeedSpreadDistance);
    }

    [Fact]
    public void SeedSpread_MaxDistance_SetsValue()
    {
        var attr = new SeedSpreadDistanceAttribute(EngineSettings.MaxSeedSpreadDistance);
        Assert.Equal(EngineSettings.MaxSeedSpreadDistance, attr.SeedSpreadDistance);
    }

    [Fact]
    public void SeedSpread_OverMax_Throws()
    {
        Assert.Throws<ApplicationException>(() =>
            new SeedSpreadDistanceAttribute(EngineSettings.MaxSeedSpreadDistance + 1));
    }

    // --- AuthorInformationAttribute ---

    [Fact]
    public void AuthorInfo_NameOnly_SetsNameAndEmptyEmail()
    {
        var attr = new AuthorInformationAttribute("TestAuthor");
        Assert.Equal("TestAuthor", attr.AuthorName);
        Assert.Equal("", attr.AuthorEmail);
    }

    [Fact]
    public void AuthorInfo_NameAndEmail_SetsBoth()
    {
        var attr = new AuthorInformationAttribute("TestAuthor", "test@example.com");
        Assert.Equal("TestAuthor", attr.AuthorName);
        Assert.Equal("test@example.com", attr.AuthorEmail);
    }

    // --- OrganismClassAttribute ---

    [Fact]
    public void OrganismClass_SetsClassName()
    {
        var attr = new OrganismClassAttribute("MyCreature");
        Assert.Equal("MyCreature", attr.ClassName);
    }

    // --- AnimalSkinAttribute ---

    [Fact]
    public void AnimalSkin_StringConstructor_DefaultsToAnt()
    {
        var attr = new AnimalSkinAttribute("custom");
        Assert.Equal(AnimalSkinFamily.Ant, attr.SkinFamily);
        Assert.Equal("custom", attr.Skin);
    }

    [Fact]
    public void AnimalSkin_FamilyConstructor_SetsFamily()
    {
        var attr = new AnimalSkinAttribute(AnimalSkinFamily.Spider);
        Assert.Equal(AnimalSkinFamily.Spider, attr.SkinFamily);
        Assert.Equal(string.Empty, attr.Skin);
    }

    [Fact]
    public void AnimalSkin_FamilyAndSkinConstructor_SetsBoth()
    {
        var attr = new AnimalSkinAttribute(AnimalSkinFamily.Beetle, "customBeetle");
        Assert.Equal(AnimalSkinFamily.Beetle, attr.SkinFamily);
        Assert.Equal("customBeetle", attr.Skin);
    }

    // --- PlantSkinAttribute ---

    [Fact]
    public void PlantSkin_StringConstructor_DefaultsToPlant()
    {
        var attr = new PlantSkinAttribute("leafy");
        Assert.Equal(PlantSkinFamily.Plant, attr.SkinFamily);
        Assert.Equal("leafy", attr.Skin);
    }

    [Fact]
    public void PlantSkin_FamilyConstructor_SetsFamily()
    {
        var attr = new PlantSkinAttribute(PlantSkinFamily.PlantTwo);
        Assert.Equal(PlantSkinFamily.PlantTwo, attr.SkinFamily);
        Assert.Equal(string.Empty, attr.Skin);
    }

    // --- MarkingColorAttribute ---

    [Fact]
    public void MarkingColor_SetsColor()
    {
        var attr = new MarkingColorAttribute(KnownColor.Red);
        Assert.Equal(KnownColor.Red, attr.MarkingColor);
    }
}
