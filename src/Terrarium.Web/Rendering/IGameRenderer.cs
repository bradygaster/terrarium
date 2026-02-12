namespace Terrarium.Web.Rendering;

/// <summary>
/// Abstraction for the game rendering surface.
/// Implemented by <see cref="CanvasGameRenderer"/> via JS interop.
/// </summary>
public interface IGameRenderer : IAsyncDisposable
{
    /// <summary>Initializes the renderer on a canvas element.</summary>
    Task InitializeAsync(int worldWidth = 5000, int worldHeight = 5000);

    /// <summary>Clears the entire canvas.</summary>
    Task ClearAsync();

    /// <summary>Draws terrain tiles across the visible viewport.</summary>
    Task DrawTerrainAsync(Dictionary<string, string>? terrainData = null);

    /// <summary>Draws a sprite at world coordinates.</summary>
    Task DrawSpriteAsync(string skinFamily, float worldX, float worldY,
        int srcX, int srcY, int frameSize = 48);

    /// <summary>Draws a text overlay at world coordinates.</summary>
    Task DrawTextAsync(string text, float worldX, float worldY,
        string? font = null, string? fillStyle = null, string? align = null);

    /// <summary>Draws a creature name label.</summary>
    Task DrawCreatureLabelAsync(string name, float worldX, float worldY, bool selected = false);

    /// <summary>Draws a status overlay at a screen-fixed position.</summary>
    Task DrawStatusOverlayAsync(string text, string position = "top-left");

    /// <summary>Renders a complete frame from world state data.</summary>
    Task RenderFrameAsync(GameRenderState worldState);

    /// <summary>Resizes the canvas.</summary>
    Task ResizeAsync(int? width = null, int? height = null);

    /// <summary>Sets the viewport position.</summary>
    Task SetViewportAsync(float x, float y);

    /// <summary>Pans the viewport by screen-space delta.</summary>
    Task PanViewportAsync(float dx, float dy);

    /// <summary>Sets the zoom level (0.25–4.0).</summary>
    Task SetZoomAsync(float level);

    /// <summary>Gets the current viewport state.</summary>
    Task<ViewportState> GetViewportAsync();

    /// <summary>Whether the renderer has been initialized.</summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Viewport state returned from the JS renderer.
/// </summary>
public sealed record ViewportState(
    float X, float Y, float Zoom,
    int CanvasWidth, int CanvasHeight);

/// <summary>
/// Serializable render state sent to JS for a full frame render.
/// </summary>
public sealed class GameRenderState
{
    public List<CreatureRenderData> Creatures { get; set; } = [];
    public Dictionary<string, string>? Terrain { get; set; }
    public string? StatusText { get; set; }
}

/// <summary>
/// Per-creature data needed for rendering a single frame.
/// </summary>
public sealed class CreatureRenderData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";
    public string SkinFamily { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int SrcX { get; set; }
    public int SrcY { get; set; }
    public int FrameSize { get; set; } = 48;
    public int Energy { get; set; }
}
