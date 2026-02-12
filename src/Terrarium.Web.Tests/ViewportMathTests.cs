namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for viewport and coordinate transformation math used by the renderer.
/// The Terrarium world is a fixed-size grid. The renderer maps world coordinates
/// to screen (canvas) coordinates through viewport transforms.
/// 
/// These tests verify the math independently of any rendering implementation,
/// ensuring the coordinate transforms are correct before pixels hit the canvas.
/// </summary>
public class ViewportMathTests
{
    // The Terrarium world size from EngineSettings
    // World is measured in cells; creatures have pixel positions within that space.
    // The viewport shows a portion of the world on screen.

    private record Viewport(
        double WorldX, double WorldY,       // Top-left corner in world coords
        double WorldWidth, double WorldHeight, // Visible area in world coords
        int ScreenWidth, int ScreenHeight);  // Canvas pixel dimensions

    private static (double screenX, double screenY) WorldToScreen(
        Viewport vp, double worldX, double worldY)
    {
        var scaleX = vp.ScreenWidth / vp.WorldWidth;
        var scaleY = vp.ScreenHeight / vp.WorldHeight;
        var sx = (worldX - vp.WorldX) * scaleX;
        var sy = (worldY - vp.WorldY) * scaleY;
        return (sx, sy);
    }

    private static (double worldX, double worldY) ScreenToWorld(
        Viewport vp, double screenX, double screenY)
    {
        var scaleX = vp.WorldWidth / vp.ScreenWidth;
        var scaleY = vp.WorldHeight / vp.ScreenHeight;
        var wx = screenX * scaleX + vp.WorldX;
        var wy = screenY * scaleY + vp.WorldY;
        return (wx, wy);
    }

    // --- World → Screen ---

    [Fact]
    public void WorldToScreen_Origin_MapsToTopLeft()
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 0, 0);
        Assert.Equal(0, sx);
        Assert.Equal(0, sy);
    }

    [Fact]
    public void WorldToScreen_BottomRight_MapsToCanvasSize()
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 1000, 1000);
        Assert.Equal(800, sx);
        Assert.Equal(600, sy);
    }

    [Fact]
    public void WorldToScreen_Center_MapsToCenterOfCanvas()
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 500, 500);
        Assert.Equal(400, sx);
        Assert.Equal(300, sy);
    }

    [Fact]
    public void WorldToScreen_WithOffset_ShiftsCoordinates()
    {
        // Viewport scrolled to (200, 100)
        var vp = new Viewport(200, 100, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 200, 100);
        Assert.Equal(0, sx);
        Assert.Equal(0, sy);
    }

    [Fact]
    public void WorldToScreen_OutsideViewport_ReturnsNegative()
    {
        var vp = new Viewport(500, 500, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 0, 0);
        Assert.True(sx < 0);
        Assert.True(sy < 0);
    }

    [Fact]
    public void WorldToScreen_Zoom_ScalesCorrectly()
    {
        // Viewing a smaller world area = zoom in
        var vp = new Viewport(0, 0, 500, 500, 800, 600);
        var (sx, sy) = WorldToScreen(vp, 250, 250);
        Assert.Equal(400, sx);
        Assert.Equal(300, sy);
    }

    // --- Screen → World ---

    [Fact]
    public void ScreenToWorld_TopLeft_MapsToViewportOrigin()
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (wx, wy) = ScreenToWorld(vp, 0, 0);
        Assert.Equal(0, wx);
        Assert.Equal(0, wy);
    }

    [Fact]
    public void ScreenToWorld_CanvasBottomRight_MapsToViewportExtent()
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (wx, wy) = ScreenToWorld(vp, 800, 600);
        Assert.Equal(1000, wx);
        Assert.Equal(1000, wy);
    }

    [Fact]
    public void ScreenToWorld_WithOffset_AccountsForScroll()
    {
        var vp = new Viewport(200, 100, 1000, 1000, 800, 600);
        var (wx, wy) = ScreenToWorld(vp, 0, 0);
        Assert.Equal(200, wx);
        Assert.Equal(100, wy);
    }

    // --- Round-trip ---

    [Theory]
    [InlineData(100, 200)]
    [InlineData(0, 0)]
    [InlineData(999, 999)]
    [InlineData(500, 300)]
    public void WorldToScreen_ScreenToWorld_RoundTrip(double wx, double wy)
    {
        var vp = new Viewport(0, 0, 1000, 1000, 800, 600);
        var (sx, sy) = WorldToScreen(vp, wx, wy);
        var (rwx, rwy) = ScreenToWorld(vp, sx, sy);

        Assert.Equal(wx, rwx, precision: 10);
        Assert.Equal(wy, rwy, precision: 10);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(400, 300)]
    [InlineData(799, 599)]
    public void ScreenToWorld_WorldToScreen_RoundTrip(double sx, double sy)
    {
        var vp = new Viewport(100, 50, 1000, 1000, 800, 600);
        var (wx, wy) = ScreenToWorld(vp, sx, sy);
        var (rsx, rsy) = WorldToScreen(vp, wx, wy);

        Assert.Equal(sx, rsx, precision: 10);
        Assert.Equal(sy, rsy, precision: 10);
    }

    // --- Terrain grid calculations ---

    [Fact]
    public void TerrainTile_WorldPosition_ToTileIndex()
    {
        // Terrain tiles are typically 48x48 pixels in world space
        const int tileSize = 48;
        int worldX = 200;
        int worldY = 150;

        int tileCol = worldX / tileSize; // 4
        int tileRow = worldY / tileSize; // 3

        Assert.Equal(4, tileCol);
        Assert.Equal(3, tileRow);
    }

    [Fact]
    public void TerrainTile_TileIndex_ToWorldPosition()
    {
        const int tileSize = 48;
        int tileCol = 4;
        int tileRow = 3;

        int worldX = tileCol * tileSize;
        int worldY = tileRow * tileSize;

        Assert.Equal(192, worldX);
        Assert.Equal(144, worldY);
    }

    [Fact]
    public void TerrainGrid_VisibleTileRange()
    {
        const int tileSize = 48;
        var vp = new Viewport(100, 50, 800, 600, 800, 600);

        // First visible tile
        int startCol = (int)(vp.WorldX / tileSize);
        int startRow = (int)(vp.WorldY / tileSize);

        // Last visible tile (inclusive)
        int endCol = (int)((vp.WorldX + vp.WorldWidth) / tileSize);
        int endRow = (int)((vp.WorldY + vp.WorldHeight) / tileSize);

        Assert.Equal(2, startCol);  // 100 / 48 = 2
        Assert.Equal(1, startRow);  // 50 / 48 = 1
        Assert.Equal(18, endCol);   // 900 / 48 = 18
        Assert.Equal(13, endRow);   // 650 / 48 = 13
    }
}
