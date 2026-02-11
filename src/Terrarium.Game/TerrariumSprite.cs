// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Sprite rendering metadata. Headless - no DirectX/WPF dependencies.
/// </summary>
public class TerrariumSprite
{
    public bool IsPlant { get; set; }
    public int FrameHeight => 48;
    public int FrameWidth => 48;
    public string? SkinFamily { get; set; }
    public float CurFrame { get; set; }
    public float CurFrameDelta { get; set; }
    public float XPosition { get; set; }
    public float YPosition { get; set; }
    public float XDelta { get; set; }
    public float YDelta { get; set; }
    public bool Selected { get; set; }
    public DisplayAction PreviousAction { get; set; }
    public string? SpriteKey { get; set; }

    public void AdvanceFrame()
    {
        if (CurFrame != 0) { XPosition += XDelta; YPosition += YDelta; }
        CurFrame = (CurFrame + CurFrameDelta) % 10;
    }
}
