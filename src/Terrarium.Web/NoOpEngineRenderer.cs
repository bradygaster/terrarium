using Terrarium.Game.Rendering;

namespace Terrarium.Web;

/// <summary>
/// No-op IEngineRenderer — the Web app renders via IGameRenderer/Canvas JS interop,
/// but GameRenderBridge requires IEngineRenderer in the DI container.
/// </summary>
internal sealed class NoOpEngineRenderer : IEngineRenderer
{
    public bool IsReady => false;

    public Task RenderWorldAsync(WorldRenderData data, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
