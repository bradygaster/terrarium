using System.Text.Json;
using Terrarium.Game.Rendering;

namespace Terrarium.Web.Tests;

/// <summary>
/// Tests against the actual animations.json file shipped in the web project.
/// Validates that the real metadata file is well-formed and matches the
/// sprite sheet layout conventions (10 frames/row, 40 rows, 8 directions, 5 actions).
/// </summary>
public class AnimationsJsonIntegrationTests
{
    private static readonly string AnimationsJsonPath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "animations.json");

    private SpriteMetadata LoadRealMetadata()
    {
        Assert.True(File.Exists(AnimationsJsonPath),
            $"animations.json not found at {AnimationsJsonPath}");
        var json = File.ReadAllText(AnimationsJsonPath);
        return SpriteMetadata.Load(json);
    }

    // --- Structural validation ---

    [Fact]
    public void AnimationsJson_Deserializes_Successfully()
    {
        var meta = LoadRealMetadata();
        Assert.NotNull(meta.Data);
    }

    [Fact]
    public void FrameSize_MatchesConvention_24_And_48()
    {
        var meta = LoadRealMetadata();
        Assert.Equal(24, meta.Data.FrameSize.Small);
        Assert.Equal(48, meta.Data.FrameSize.Large);
    }

    [Fact]
    public void AllAnimalCreatures_Have_Large_And_Small_SheetRefs()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            if (creature.Type != "animal") continue;

            Assert.NotNull(creature.SpriteSheet.Small);
            Assert.NotNull(creature.SpriteSheet.Large);
            Assert.EndsWith(".bmp", creature.SpriteSheet.Small);
            Assert.EndsWith(".bmp", creature.SpriteSheet.Large);
        }
    }

    [Fact]
    public void EffectCreatures_MayOmit_SmallSheet()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            // Effects (e.g. teleporter) may only have a large sheet
            Assert.NotNull(creature.SpriteSheet.Large);
        }
    }

    // --- Layout conventions ---

    private static readonly string[] ExpectedDirections =
        ["e", "se", "s", "sw", "w", "nw", "n", "ne"];

    private static readonly string[] ActionKeys =
        ["attacked", "defended", "died", "ate", "moved"];

    [Fact]
    public void AllAnimalCreatures_Have_AllDirectionalAnimations()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            if (creature.Type != "animal") continue;

            foreach (var action in ActionKeys)
            {
                foreach (var dir in ExpectedDirections)
                {
                    var key = $"{action}_{dir}";
                    var anim = creature.Animations.GetValueOrDefault(key);
                    Assert.True(anim is not null,
                        $"Missing animation '{key}' for family '{family}'");
                }
            }
        }
    }

    [Fact]
    public void AllAnimations_Have_PositiveFrameCount()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            foreach (var (key, anim) in creature.Animations)
            {
                Assert.True(anim.FrameCount > 0,
                    $"{family}/{key}: FrameCount must be > 0");
            }
        }
    }

    [Fact]
    public void AllAnimations_StartFrame_PlusFrameCount_DoesNotExceed_SheetWidth()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            var sheetWidth = creature.SheetSize.Large?.Width ?? 0;
            var frameSize = meta.Data.FrameSize.Large;
            int maxFramesPerRow = sheetWidth > 0 ? sheetWidth / frameSize : 10;

            foreach (var (key, anim) in creature.Animations)
            {
                Assert.True(anim.StartFrame + anim.FrameCount <= maxFramesPerRow,
                    $"{family}/{key}: StartFrame({anim.StartFrame}) + FrameCount({anim.FrameCount}) exceeds {maxFramesPerRow} frames/row");
            }
        }
    }

    [Fact]
    public void AllAnimations_RowWithinSheet_LargeDimensions()
    {
        var meta = LoadRealMetadata();
        var frameSize = meta.Data.FrameSize.Large;

        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            var sheetHeight = creature.SheetSize.Large?.Height ?? 0;
            Assert.True(sheetHeight > 0, $"{family}: Missing large sheet dimensions");

            foreach (var (key, anim) in creature.Animations)
            {
                var pixelY = (anim.Row + 1) * frameSize;
                Assert.True(pixelY <= sheetHeight,
                    $"{family}/{key}: Row {anim.Row} at {pixelY}px exceeds sheet height {sheetHeight}px");
            }
        }
    }

    [Fact]
    public void AllAnimations_FrameDuration_IsPositive()
    {
        var meta = LoadRealMetadata();
        foreach (var family in meta.CreatureFamilies)
        {
            var creature = meta.GetCreature(family)!;
            foreach (var (key, anim) in creature.Animations)
            {
                Assert.True(anim.FrameDuration > 0,
                    $"{family}/{key}: FrameDuration must be > 0");
            }
        }
    }

    // --- Direction ↔ row mapping (layout convention from animations.json) ---

    [Theory]
    [InlineData("attacked", "e", 0)]
    [InlineData("attacked", "se", 1)]
    [InlineData("attacked", "s", 2)]
    [InlineData("attacked", "sw", 3)]
    [InlineData("attacked", "w", 4)]
    [InlineData("attacked", "nw", 5)]
    [InlineData("attacked", "n", 6)]
    [InlineData("attacked", "ne", 7)]
    [InlineData("defended", "e", 8)]
    [InlineData("defended", "ne", 15)]
    [InlineData("died", "e", 16)]
    [InlineData("ate", "e", 24)]
    [InlineData("moved", "e", 32)]
    [InlineData("moved", "ne", 39)]
    public void DirectionRowMapping_MatchesConvention(string action, string direction, int expectedRow)
    {
        var meta = LoadRealMetadata();
        var anim = meta.GetAnimation("ant", $"{action}_{direction}");
        Assert.NotNull(anim);
        Assert.Equal(expectedRow, anim.Row);
    }

    // --- Sheet dimensions math ---

    [Theory]
    [InlineData("ant", 480, 1920)]
    [InlineData("beetle", 480, 1920)]
    [InlineData("spider", 480, 1920)]
    [InlineData("scorpion", 480, 1920)]
    [InlineData("inchworm", 480, 1920)]
    public void AnimalSheet_LargeDimensions_Match_10x40_At48px(
        string family, int expectedWidth, int expectedHeight)
    {
        var meta = LoadRealMetadata();
        var creature = meta.GetCreature(family);
        if (creature is null) return; // Skip if family not in metadata yet

        Assert.Equal(expectedWidth, creature.SheetSize.Large!.Width);
        Assert.Equal(expectedHeight, creature.SheetSize.Large!.Height);
    }

    [Theory]
    [InlineData("ant", 240, 960)]
    [InlineData("beetle", 240, 960)]
    public void AnimalSheet_SmallDimensions_Match_10x40_At24px(
        string family, int expectedWidth, int expectedHeight)
    {
        var meta = LoadRealMetadata();
        var creature = meta.GetCreature(family);
        if (creature is null) return;

        Assert.Equal(expectedWidth, creature.SheetSize.Small!.Width);
        Assert.Equal(expectedHeight, creature.SheetSize.Small!.Height);
    }

    // --- GetFrameRect with real data ---

    [Fact]
    public void GetFrameRect_Ant_MovedNE_Frame0_CorrectPixels()
    {
        var meta = LoadRealMetadata();
        // moved base row = 32, NE direction index = 7 → row 39
        var rect = meta.GetFrameRect("ant", "moved_ne", 0, useLarge: true);

        Assert.NotNull(rect);
        Assert.Equal(0, rect.Value.X);
        Assert.Equal(39 * 48, rect.Value.Y); // row 39 * 48px
        Assert.Equal(48, rect.Value.Width);
        Assert.Equal(48, rect.Value.Height);
    }

    [Fact]
    public void GetFrameRect_Ant_AttackedSE_Frame3_CorrectPixels()
    {
        var meta = LoadRealMetadata();
        // attacked base row = 0, SE direction index = 1 → row 1
        var rect = meta.GetFrameRect("ant", "attacked_se", 3, useLarge: true);

        Assert.NotNull(rect);
        Assert.Equal(3 * 48, rect.Value.X);
        Assert.Equal(1 * 48, rect.Value.Y);
    }
}
