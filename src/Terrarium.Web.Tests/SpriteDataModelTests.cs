using Terrarium.Game.Rendering;

namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for the sprite data model records: AnimationSequence, SpriteSheetRef,
/// FrameSizeInfo, SheetDimensions, SheetSizeInfo, CreatureAnimations, SpriteAnimationData.
/// These are the C# types that represent the animations.json schema.
/// </summary>
public class SpriteDataModelTests
{
    // --- FrameSizeInfo ---

    [Fact]
    public void FrameSizeInfo_RecordEquality()
    {
        var a = new FrameSizeInfo(24, 48);
        var b = new FrameSizeInfo(24, 48);
        Assert.Equal(a, b);
    }

    [Fact]
    public void FrameSizeInfo_Properties()
    {
        var info = new FrameSizeInfo(24, 48);
        Assert.Equal(24, info.Small);
        Assert.Equal(48, info.Large);
    }

    // --- SheetDimensions ---

    [Fact]
    public void SheetDimensions_RecordEquality()
    {
        var a = new SheetDimensions(480, 1920);
        var b = new SheetDimensions(480, 1920);
        Assert.Equal(a, b);
    }

    [Fact]
    public void SheetDimensions_Properties()
    {
        var dim = new SheetDimensions(480, 1920);
        Assert.Equal(480, dim.Width);
        Assert.Equal(1920, dim.Height);
    }

    // --- SpriteSheetRef ---

    [Fact]
    public void SpriteSheetRef_NullableFields()
    {
        var noSmall = new SpriteSheetRef(null, "large.bmp");
        Assert.Null(noSmall.Small);
        Assert.Equal("large.bmp", noSmall.Large);
    }

    [Fact]
    public void SpriteSheetRef_BothSizes()
    {
        var both = new SpriteSheetRef("ant24.bmp", "ant48.bmp");
        Assert.Equal("ant24.bmp", both.Small);
        Assert.Equal("ant48.bmp", both.Large);
    }

    // --- SheetSizeInfo ---

    [Fact]
    public void SheetSizeInfo_BothVariants()
    {
        var info = new SheetSizeInfo(
            new SheetDimensions(240, 960),
            new SheetDimensions(480, 1920));

        Assert.Equal(240, info.Small!.Width);
        Assert.Equal(960, info.Small!.Height);
        Assert.Equal(480, info.Large!.Width);
        Assert.Equal(1920, info.Large!.Height);
    }

    [Fact]
    public void SheetSizeInfo_NullSmall()
    {
        var info = new SheetSizeInfo(null, new SheetDimensions(480, 1920));
        Assert.Null(info.Small);
        Assert.NotNull(info.Large);
    }

    // --- AnimationSequence ---

    [Fact]
    public void AnimationSequence_AllProperties()
    {
        var seq = new AnimationSequence(
            Row: 5,
            StartFrame: 0,
            FrameCount: 10,
            FrameDuration: 80,
            Loop: false);

        Assert.Equal(5, seq.Row);
        Assert.Equal(0, seq.StartFrame);
        Assert.Equal(10, seq.FrameCount);
        Assert.Equal(80, seq.FrameDuration);
        Assert.False(seq.Loop);
    }

    [Fact]
    public void AnimationSequence_RecordEquality()
    {
        var a = new AnimationSequence(1, 0, 10, 80, false);
        var b = new AnimationSequence(1, 0, 10, 80, false);
        Assert.Equal(a, b);
    }

    [Fact]
    public void AnimationSequence_Inequality_DifferentRow()
    {
        var a = new AnimationSequence(1, 0, 10, 80, false);
        var b = new AnimationSequence(2, 0, 10, 80, false);
        Assert.NotEqual(a, b);
    }

    // --- CreatureAnimations ---

    [Fact]
    public void CreatureAnimations_Properties()
    {
        var animations = new Dictionary<string, AnimationSequence>
        {
            ["idle"] = new AnimationSequence(0, 0, 1, 500, false),
            ["moved_n"] = new AnimationSequence(38, 0, 10, 60, true)
        };

        var creature = new CreatureAnimations(
            Type: "animal",
            SpriteSheet: new SpriteSheetRef("ant24.bmp", "ant48.bmp"),
            SheetSize: new SheetSizeInfo(
                new SheetDimensions(240, 960),
                new SheetDimensions(480, 1920)),
            Animations: animations);

        Assert.Equal("animal", creature.Type);
        Assert.Equal(2, creature.Animations.Count);
        Assert.True(creature.Animations.ContainsKey("idle"));
        Assert.True(creature.Animations.ContainsKey("moved_n"));
    }

    // --- SpriteAnimationData ---

    [Fact]
    public void SpriteAnimationData_EmptyCreatures()
    {
        var data = new SpriteAnimationData(
            FrameSize: new FrameSizeInfo(24, 48),
            Creatures: new Dictionary<string, CreatureAnimations>());

        Assert.Empty(data.Creatures);
        Assert.Equal(24, data.FrameSize.Small);
    }

    // --- Frame rect math verification (manual calculation) ---

    [Theory]
    [InlineData(0, 0, 10, 0, 48, 0, 0)]       // Frame 0, row 0 → (0, 0)
    [InlineData(5, 3, 10, 0, 48, 240, 144)]    // Frame 5, row 3 → (240, 144)
    [InlineData(9, 39, 10, 0, 48, 432, 1872)]  // Frame 9, row 39 → (432, 1872)
    [InlineData(0, 0, 10, 0, 24, 0, 0)]        // Small: Frame 0, row 0 → (0, 0)
    [InlineData(5, 3, 10, 0, 24, 120, 72)]     // Small: Frame 5, row 3 → (120, 72)
    public void FrameRect_ManualMath(
        int frameIndex, int row, int frameCount, int startFrame,
        int frameSize, int expectedX, int expectedY)
    {
        // Replicate the frame rect formula from SpriteMetadata.GetFrameRect
        var frame = startFrame + (frameIndex % frameCount);
        var x = frame * frameSize;
        var y = row * frameSize;

        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }
}
