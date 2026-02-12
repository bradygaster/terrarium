using Terrarium.Game;
using OrganismBase;

namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for TerrariumSprite — the headless sprite rendering metadata
/// that tracks position, frame, and animation state.
/// </summary>
public class TerrariumSpriteTests
{
    // --- Defaults ---

    [Fact]
    public void NewSprite_HasDefaultValues()
    {
        var sprite = new TerrariumSprite();

        Assert.Equal(0f, sprite.CurFrame);
        Assert.Equal(0f, sprite.CurFrameDelta);
        Assert.Equal(0f, sprite.XPosition);
        Assert.Equal(0f, sprite.YPosition);
        Assert.Equal(0f, sprite.XDelta);
        Assert.Equal(0f, sprite.YDelta);
        Assert.False(sprite.IsPlant);
        Assert.False(sprite.Selected);
        Assert.Null(sprite.SkinFamily);
        Assert.Null(sprite.SpriteKey);
    }

    // --- Frame dimensions ---

    [Fact]
    public void FrameWidth_Is48()
    {
        var sprite = new TerrariumSprite();
        Assert.Equal(48, sprite.FrameWidth);
    }

    [Fact]
    public void FrameHeight_Is48()
    {
        var sprite = new TerrariumSprite();
        Assert.Equal(48, sprite.FrameHeight);
    }

    // --- AdvanceFrame ---

    [Fact]
    public void AdvanceFrame_FromZero_DoesNotMovePosition()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 0,
            CurFrameDelta = 1,
            XDelta = 5,
            YDelta = 3,
            XPosition = 100,
            YPosition = 200
        };

        sprite.AdvanceFrame();

        // When CurFrame == 0, position should NOT change
        Assert.Equal(100f, sprite.XPosition);
        Assert.Equal(200f, sprite.YPosition);
        // But CurFrame advances
        Assert.Equal(1f, sprite.CurFrame);
    }

    [Fact]
    public void AdvanceFrame_FromNonZero_MovesPosition()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 1,
            CurFrameDelta = 1,
            XDelta = 5,
            YDelta = 3,
            XPosition = 100,
            YPosition = 200
        };

        sprite.AdvanceFrame();

        Assert.Equal(105f, sprite.XPosition);
        Assert.Equal(203f, sprite.YPosition);
        Assert.Equal(2f, sprite.CurFrame);
    }

    [Fact]
    public void AdvanceFrame_WrapsAt10()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 9,
            CurFrameDelta = 1
        };

        sprite.AdvanceFrame();

        // (9 + 1) % 10 = 0
        Assert.Equal(0f, sprite.CurFrame);
    }

    [Fact]
    public void AdvanceFrame_FractionalDelta()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 0,
            CurFrameDelta = 0.5f
        };

        sprite.AdvanceFrame();
        Assert.Equal(0.5f, sprite.CurFrame);

        sprite.AdvanceFrame();
        Assert.Equal(1.0f, sprite.CurFrame);
    }

    [Fact]
    public void AdvanceFrame_MultipleFrames_AccumulatesPosition()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 1,
            CurFrameDelta = 1,
            XDelta = 2,
            YDelta = -1,
            XPosition = 0,
            YPosition = 0
        };

        // Advance 5 frames (all from non-zero CurFrame)
        for (int i = 0; i < 5; i++)
        {
            sprite.AdvanceFrame();
        }

        // Position moves each frame: 5 * 2 = 10 X, 5 * (-1) = -5 Y
        Assert.Equal(10f, sprite.XPosition);
        Assert.Equal(-5f, sprite.YPosition);
    }

    [Fact]
    public void AdvanceFrame_FullCycle_10Frames_ResetsToZero()
    {
        var sprite = new TerrariumSprite
        {
            CurFrame = 0,
            CurFrameDelta = 1
        };

        for (int i = 0; i < 10; i++)
        {
            sprite.AdvanceFrame();
        }

        Assert.Equal(0f, sprite.CurFrame);
    }

    // --- Property setters ---

    [Fact]
    public void SkinFamily_CanBeSetAndRead()
    {
        var sprite = new TerrariumSprite { SkinFamily = "ant" };
        Assert.Equal("ant", sprite.SkinFamily);
    }

    [Fact]
    public void IsPlant_CanBeSetAndRead()
    {
        var sprite = new TerrariumSprite { IsPlant = true };
        Assert.True(sprite.IsPlant);
    }

    [Fact]
    public void PreviousAction_DefaultsToFirstEnumValue()
    {
        var sprite = new TerrariumSprite();
        // DisplayAction.Attacked is -1, which is the first defined value
        // Default enum value is 0, which doesn't correspond to a named member
        Assert.Equal(default(DisplayAction), sprite.PreviousAction);
    }

    [Fact]
    public void PreviousAction_CanBeSetToMoved()
    {
        var sprite = new TerrariumSprite { PreviousAction = DisplayAction.Moved };
        Assert.Equal(DisplayAction.Moved, sprite.PreviousAction);
    }

    [Fact]
    public void Selected_CanBeToggled()
    {
        var sprite = new TerrariumSprite { Selected = true };
        Assert.True(sprite.Selected);
        sprite.Selected = false;
        Assert.False(sprite.Selected);
    }
}
