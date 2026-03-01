using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Terrarium.Web.Rendering;

/// <summary>
/// Canvas-based implementation of <see cref="IGameRenderer"/>.
/// Delegates all rendering to terrarium-renderer.js via JS interop.
/// </summary>
public sealed class CanvasGameRenderer : IGameRenderer
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<CanvasGameRenderer>? _dotNetRef;
    private ElementReference _canvasRef;

    public bool IsInitialized { get; private set; }

    /// <summary>Fires when a creature is clicked in the viewport.</summary>
    public event Func<string, string, string, Task>? OnCreatureSelected;

    /// <summary>Fires when the selection is cleared.</summary>
    public event Func<Task>? OnCreatureDeselected;

    public CanvasGameRenderer(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Sets the canvas element reference from the Blazor component.
    /// Must be called before <see cref="InitializeAsync"/>.
    /// </summary>
    public void SetCanvas(ElementReference canvasRef)
    {
        _canvasRef = canvasRef;
    }

    public async Task InitializeAsync(int worldWidth = 5000, int worldHeight = 5000)
    {
        _module = await _js.InvokeAsync<IJSObjectReference>(
            "import", "./js/terrarium-renderer.js");
        _dotNetRef = DotNetObjectReference.Create(this);

        await _module.InvokeVoidAsync("initialize", _canvasRef, _dotNetRef, worldWidth, worldHeight);
        IsInitialized = true;
    }

    public async Task ClearAsync()
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("clear");
    }

    public async Task DrawTerrainAsync(Dictionary<string, string>? terrainData = null)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("drawTerrain", terrainData);
    }

    public async Task DrawSpriteAsync(string skinFamily, float worldX, float worldY,
        int srcX, int srcY, int frameSize = 48)
    {
        // Sprite rendering is handled by renderFrame; this is for individual draws
        if (_module is not null)
            await _module.InvokeVoidAsync("drawSprite", null, srcX, srcY, frameSize, frameSize,
                worldX, worldY, frameSize, frameSize);
    }

    public async Task DrawTextAsync(string text, float worldX, float worldY,
        string? font = null, string? fillStyle = null, string? align = null)
    {
        if (_module is not null)
        {
            var options = new { font, fillStyle, align };
            await _module.InvokeVoidAsync("drawText", text, worldX, worldY, options);
        }
    }

    public async Task DrawCreatureLabelAsync(string name, float worldX, float worldY, bool selected = false)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("drawCreatureLabel", name, worldX, worldY, selected);
    }

    public async Task DrawStatusOverlayAsync(string text, string position = "top-left")
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("drawStatusOverlay", text, position);
    }

    public async Task RenderFrameAsync(GameRenderState worldState)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("renderFrame", worldState, null);
    }

    public async Task ResizeAsync(int? width = null, int? height = null)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("resize", width, height);
    }

    public async Task SetViewportAsync(float x, float y)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("setViewport", x, y);
    }

    public async Task PanViewportAsync(float dx, float dy)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("panViewport", dx, dy);
    }

    public async Task SetZoomAsync(float level)
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("setZoom", level);
    }

    public async Task<ViewportState> GetViewportAsync()
    {
        if (_module is not null)
            return await _module.InvokeAsync<ViewportState>("getViewport");
        return new ViewportState(0, 0, 1, 0, 0);
    }

    [JSInvokable]
    public async Task OnCreatureSelected_JS(string id, string name, string species)
    {
        if (OnCreatureSelected is not null)
            await OnCreatureSelected.Invoke(id, name, species);
    }

    [JSInvokable]
    public async Task OnCreatureDeselected_JS()
    {
        if (OnCreatureDeselected is not null)
            await OnCreatureDeselected.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected — JS cleanup is unnecessary
            }
            await _module.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}
