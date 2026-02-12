using Terrarium.Game.Rendering;

namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for SpriteMetadata — the C# service that loads animations.json
/// and computes frame rectangles for sprite sheet rendering.
/// </summary>
public class SpriteMetadataTests
{
    private const string MinimalJson = """
    {
        "frameSize": { "small": 24, "large": 48 },
        "creatures": {
            "ant": {
                "type": "animal",
                "spriteSheet": { "small": "ant24.bmp", "large": "ant48.bmp" },
                "sheetSize": {
                    "small": { "width": 240, "height": 960 },
                    "large": { "width": 480, "height": 1920 }
                },
                "animations": {
                    "idle": {
                        "row": 38,
                        "startFrame": 0,
                        "frameCount": 1,
                        "frameDuration": 500,
                        "loop": false
                    },
                    "moved_ne": {
                        "row": 39,
                        "startFrame": 0,
                        "frameCount": 10,
                        "frameDuration": 60,
                        "loop": true
                    },
                    "attacked_se": {
                        "row": 1,
                        "startFrame": 0,
                        "frameCount": 10,
                        "frameDuration": 80,
                        "loop": false
                    }
                }
            },
            "plant": {
                "type": "plant",
                "spriteSheet": { "small": "plant24.bmp", "large": "plant48.bmp" },
                "sheetSize": {
                    "small": { "width": 240, "height": 48 },
                    "large": { "width": 480, "height": 96 }
                },
                "animations": {
                    "idle": {
                        "row": 0,
                        "startFrame": 0,
                        "frameCount": 1,
                        "frameDuration": 1000,
                        "loop": false
                    }
                }
            }
        }
    }
    """;

    private SpriteMetadata LoadMinimal() => SpriteMetadata.Load(MinimalJson);

    // --- Load / Deserialize ---

    [Fact]
    public void Load_ValidJson_ReturnsNonNull()
    {
        var meta = LoadMinimal();
        Assert.NotNull(meta);
        Assert.NotNull(meta.Data);
    }

    [Fact]
    public void Load_InvalidJson_Throws()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => SpriteMetadata.Load("not json"));
    }

    [Fact]
    public void Load_EmptyCreatures_Throws()
    {
        var json = """{ "frameSize": { "small": 24, "large": 48 }, "creatures": {} }""";
        var meta = SpriteMetadata.Load(json);
        Assert.Empty(meta.CreatureFamilies);
    }

    // --- FrameSize ---

    [Fact]
    public void FrameSize_Small_Is24()
    {
        var meta = LoadMinimal();
        Assert.Equal(24, meta.Data.FrameSize.Small);
    }

    [Fact]
    public void FrameSize_Large_Is48()
    {
        var meta = LoadMinimal();
        Assert.Equal(48, meta.Data.FrameSize.Large);
    }

    // --- CreatureFamilies ---

    [Fact]
    public void CreatureFamilies_ContainsExpectedEntries()
    {
        var meta = LoadMinimal();
        var families = meta.CreatureFamilies.ToList();
        Assert.Contains("ant", families);
        Assert.Contains("plant", families);
        Assert.Equal(2, families.Count);
    }

    // --- GetCreature ---

    [Fact]
    public void GetCreature_KnownFamily_ReturnsData()
    {
        var meta = LoadMinimal();
        var ant = meta.GetCreature("ant");
        Assert.NotNull(ant);
        Assert.Equal("animal", ant.Type);
    }

    [Fact]
    public void GetCreature_CaseInsensitive()
    {
        var meta = LoadMinimal();
        var ant = meta.GetCreature("ANT");
        Assert.NotNull(ant);
    }

    [Fact]
    public void GetCreature_UnknownFamily_ReturnsNull()
    {
        var meta = LoadMinimal();
        Assert.Null(meta.GetCreature("dragon"));
    }

    // --- SpriteSheet refs ---

    [Fact]
    public void SpriteSheetRef_ReturnsCorrectFilenames()
    {
        var meta = LoadMinimal();
        var ant = meta.GetCreature("ant")!;
        Assert.Equal("ant24.bmp", ant.SpriteSheet.Small);
        Assert.Equal("ant48.bmp", ant.SpriteSheet.Large);
    }

    [Fact]
    public void SheetSize_MatchesExpectedDimensions()
    {
        var meta = LoadMinimal();
        var ant = meta.GetCreature("ant")!;

        // 10 frames * 48px = 480 wide, 40 rows * 48px = 1920 tall
        Assert.Equal(480, ant.SheetSize.Large!.Width);
        Assert.Equal(1920, ant.SheetSize.Large!.Height);

        // 10 frames * 24px = 240 wide, 40 rows * 24px = 960 tall
        Assert.Equal(240, ant.SheetSize.Small!.Width);
        Assert.Equal(960, ant.SheetSize.Small!.Height);
    }

    // --- GetAnimation ---

    [Fact]
    public void GetAnimation_KnownAction_ReturnsSequence()
    {
        var meta = LoadMinimal();
        var anim = meta.GetAnimation("ant", "idle");
        Assert.NotNull(anim);
        Assert.Equal(38, anim.Row);
        Assert.Equal(1, anim.FrameCount);
    }

    [Fact]
    public void GetAnimation_CaseInsensitive()
    {
        var meta = LoadMinimal();
        Assert.NotNull(meta.GetAnimation("ANT", "IDLE"));
    }

    [Fact]
    public void GetAnimation_UnknownAction_ReturnsNull()
    {
        var meta = LoadMinimal();
        Assert.Null(meta.GetAnimation("ant", "flying"));
    }

    [Fact]
    public void GetAnimation_UnknownFamily_ReturnsNull()
    {
        var meta = LoadMinimal();
        Assert.Null(meta.GetAnimation("dragon", "idle"));
    }

    // --- AnimationSequence properties ---

    [Fact]
    public void AnimationSequence_MovedNE_HasCorrectMetadata()
    {
        var meta = LoadMinimal();
        var anim = meta.GetAnimation("ant", "moved_ne")!;
        Assert.Equal(39, anim.Row);
        Assert.Equal(0, anim.StartFrame);
        Assert.Equal(10, anim.FrameCount);
        Assert.Equal(60, anim.FrameDuration);
        Assert.True(anim.Loop);
    }

    [Fact]
    public void AnimationSequence_AttackedSE_NonLooping()
    {
        var meta = LoadMinimal();
        var anim = meta.GetAnimation("ant", "attacked_se")!;
        Assert.False(anim.Loop);
        Assert.Equal(80, anim.FrameDuration);
    }

    // --- GetFrameRect — the core sprite sheet math ---

    [Fact]
    public void GetFrameRect_Frame0_Large_CorrectPixelCoords()
    {
        var meta = LoadMinimal();
        // ant idle: row 38, startFrame 0, frameCount 1
        var rect = meta.GetFrameRect("ant", "idle", 0, useLarge: true);

        Assert.NotNull(rect);
        // frame col = startFrame + (0 % 1) = 0 → x = 0 * 48 = 0
        // row 38 → y = 38 * 48 = 1824
        Assert.Equal(0, rect.Value.X);
        Assert.Equal(1824, rect.Value.Y);
        Assert.Equal(48, rect.Value.Width);
        Assert.Equal(48, rect.Value.Height);
    }

    [Fact]
    public void GetFrameRect_Frame0_Small_CorrectPixelCoords()
    {
        var meta = LoadMinimal();
        var rect = meta.GetFrameRect("ant", "idle", 0, useLarge: false);

        Assert.NotNull(rect);
        // x = 0 * 24 = 0, y = 38 * 24 = 912
        Assert.Equal(0, rect.Value.X);
        Assert.Equal(912, rect.Value.Y);
        Assert.Equal(24, rect.Value.Width);
        Assert.Equal(24, rect.Value.Height);
    }

    [Fact]
    public void GetFrameRect_Frame5_Large_MovedNE()
    {
        var meta = LoadMinimal();
        // moved_ne: row 39, startFrame 0, frameCount 10
        var rect = meta.GetFrameRect("ant", "moved_ne", 5, useLarge: true);

        Assert.NotNull(rect);
        // frame col = 0 + (5 % 10) = 5 → x = 5 * 48 = 240
        // row 39 → y = 39 * 48 = 1872
        Assert.Equal(240, rect.Value.X);
        Assert.Equal(1872, rect.Value.Y);
        Assert.Equal(48, rect.Value.Width);
        Assert.Equal(48, rect.Value.Height);
    }

    [Fact]
    public void GetFrameRect_FrameWraps_WhenExceedsFrameCount()
    {
        var meta = LoadMinimal();
        // moved_ne: frameCount 10, so frame 12 should wrap to frame 2
        var rect = meta.GetFrameRect("ant", "moved_ne", 12, useLarge: true);

        Assert.NotNull(rect);
        // frame col = 0 + (12 % 10) = 2 → x = 2 * 48 = 96
        Assert.Equal(96, rect.Value.X);
    }

    [Fact]
    public void GetFrameRect_SingleFrameAnimation_AlwaysSameRect()
    {
        var meta = LoadMinimal();
        // idle has frameCount 1 → any frameIndex % 1 == 0
        var rect0 = meta.GetFrameRect("ant", "idle", 0);
        var rect5 = meta.GetFrameRect("ant", "idle", 5);
        var rect99 = meta.GetFrameRect("ant", "idle", 99);

        Assert.Equal(rect0, rect5);
        Assert.Equal(rect0, rect99);
    }

    [Fact]
    public void GetFrameRect_UnknownAction_ReturnsNull()
    {
        var meta = LoadMinimal();
        Assert.Null(meta.GetFrameRect("ant", "flying", 0));
    }

    [Fact]
    public void GetFrameRect_UnknownFamily_ReturnsNull()
    {
        var meta = LoadMinimal();
        Assert.Null(meta.GetFrameRect("dragon", "idle", 0));
    }

    [Fact]
    public void GetFrameRect_DefaultsToLargeFrames()
    {
        var meta = LoadMinimal();
        // Default useLarge = true
        var rect = meta.GetFrameRect("ant", "idle", 0);
        Assert.Equal(48, rect!.Value.Width);
    }

    // --- Plant sprite ---

    [Fact]
    public void GetFrameRect_Plant_Idle_CorrectRect()
    {
        var meta = LoadMinimal();
        var rect = meta.GetFrameRect("plant", "idle", 0, useLarge: true);

        Assert.NotNull(rect);
        Assert.Equal(0, rect.Value.X);
        Assert.Equal(0, rect.Value.Y);
        Assert.Equal(48, rect.Value.Width);
    }
}
