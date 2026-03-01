# Journal Entry #15 — They Move

> **Date:** February 12, 2026 — Post-Sprint 14  
> **Author:** Beth (Technical Writer)  
> **Status:** The creatures are alive. Not "the tests pass" alive. Not "the CI is green" alive. Actually, visually, *on-screen* alive. Moving. Eating. Dying. With proper sprites. With transparency. In a browser. Brady is losing his mind.

---

## "I literally cannot believe it."

That's the quote. That's the whole story, really.

But let me tell you *how* we got from "blank green canvas with an error in the console" to "ants chasing beetles across a living ecosystem rendered at 60 FPS in a browser."

It took four bugs. Four bugs that were invisible to every test, every build, every CI run. Four bugs that only revealed themselves when a human opened a browser and said, "Why is the map empty?"

---

## Bug #1: The Thread That Talked to Nobody

Here's the setup. The `EcosystemSimulationWorker` on the server broadcasts a `WorldStateUpdate` every 500ms via SignalR. The `TerrariumHubClient` in the Blazor app receives it and fires an event. The `Home.razor` component handles that event, maps the data, and calls `_gameView.RenderFrameAsync()`.

Here's the problem: **SignalR callbacks run on thread pool threads.** Blazor Server JS interop requires the Blazor synchronization context. The `RenderFrameAsync` call — which does JS interop to draw on the canvas — was executing on the wrong thread.

It didn't throw. It didn't warn. It just... didn't render.

```csharp
// Before: runs on thread pool — JS interop silently fails
private async Task HandleWorldStateUpdate(WorldStateUpdate update)
{
    // ... build render state ...
    await _gameView.RenderFrameAsync(renderState);  // ← wrong thread
    await InvokeAsync(StateHasChanged);
}

// After: everything runs on the Blazor sync context
private async Task HandleWorldStateUpdate(WorldStateUpdate update)
{
    await InvokeAsync(async () =>
    {
        // ... build render state ...
        await _gameView.RenderFrameAsync(renderState);  // ← correct thread
        StateHasChanged();
    });
}
```

One `InvokeAsync` wrapper. That's it. That's the difference between "nothing renders" and "everything renders."

The lesson: in Blazor Server, if your SignalR callback does JS interop, you *must* marshal onto the synchronization context. The framework won't do it for you. And it won't tell you it failed.

---

## Bug #2: The Sprites That Were Never Loaded

The `terrarium-renderer.js` module has a `renderFrame` function. For each creature, it calls `SpriteManager.drawSprite()` to draw the creature's sprite from the loaded sprite sheets.

But `SpriteManager.preloadAll()` — the function that actually loads the BMP sprite sheets into memory — was never called. Not during initialization. Not before the first frame. Not ever.

The `initialize()` function loaded terrain tiles. It loaded the teleporter sprite. It bound mouse events. It set up the viewport. It did *everything except load the creature sprites*.

```javascript
// Added to initialize():
if (typeof SpriteManager !== 'undefined') {
    await SpriteManager.loadManifest();
    await SpriteManager.preloadAll(true);
}
```

`drawSprite()` returned `false` (no sheets cached). The fallback path required a `spriteSheets` parameter that C# always passed as `null`. So nothing drew. No errors. No warnings. Just an empty canvas with nice green terrain.

---

## Bug #3: The Pink Squares of Doom

Once sprites were loading and rendering, we hit the next problem immediately.

The creatures appeared! But they were... pink. Hot pink. Magenta squares running around the ecosystem like a rave at a garden party.

The original .NET Terrarium (circa 2002) used BMP sprite sheets. BMPs don't support alpha transparency. So the artists used the classic game dev trick: paint the transparent areas magenta (`#FF00FF`), and the renderer treats that color as "don't draw this pixel."

The web renderer was loading BMPs via `createImageBitmap()` — which faithfully preserves every pixel, including the magenta. The browser doesn't know it's supposed to be transparent. It's just a color.

The fix: post-process every loaded sprite sheet. Draw it to an offscreen canvas, scan every pixel, and set alpha to 0 for anything matching the magenta key color:

```javascript
const imageData = ctx.getImageData(0, 0, width, height);
const data = imageData.data;
for (let i = 0; i < data.length; i += 4) {
    if (data[i] === 255 && data[i + 1] === 0 && data[i + 2] === 255) {
        data[i + 3] = 0;  // magenta → transparent
    }
}
```

From pink squares to proper transparent sprites. Twenty-five-year-old art assets, rendering correctly in a modern browser, because we taught the loader about a transparency convention from 2002.

---

## Bug #4: The Invisible World

Even with sprites loading correctly, the first time Brady ran the app, the map was green and empty.

There are 2,500+ creatures in the world. The world is 5,000×5,000 pixels. The viewport starts at position (0, 0) and shows roughly 1,000×700 pixels of the world.

That's 2.8% of the total area. With creatures randomly distributed, the viewport was staring at an empty corner while thousands of creatures lived and died offscreen.

The fix: on the first `WorldStateUpdate`, center the viewport on the average position of all creatures:

```csharp
if (!_viewportCentered && update.Creatures?.Count > 0)
{
    var avgX = update.Creatures.Average(c => c.X);
    var avgY = update.Creatures.Average(c => c.Y);
    await _gameView.Renderer.SetViewportAsync(
        (float)avgX - 400, (float)avgY - 300);
    _viewportCentered = true;
}
```

Open the app, and you're immediately looking at the action. Herbivores grazing. Carnivores hunting. Plants growing. Life happening.

---

## The Moment

Brady opened the browser. The terrain rendered. A beat passed — the SignalR connection established. And then:

Movement.

Ants scurrying across the grass. Beetles meandering between plants. Scorpions pursuing prey. Inchworms inching. Plants sitting there being plants (they're plants, what do you want).

Names floating above creatures. Energy bars depleting. Population counts ticking upward in the sidebar. The status bar showing "Tick 847 — 2,631 organisms."

"Is this real?" Brady asked.

"I literally cannot believe it."

And then he dragged the viewport. Panned across the world. Saw the teleport zones glowing at the edges. Clicked on a creature and saw its stats. Watched a carnivore chase down a herbivore, kill it, and eat it.

"This is SO COOL."

Twenty-five years. From DirectX 7 to HTML5 Canvas. From magenta transparency keys to `imageData.data[i + 3] = 0`. From Windows-only desktop app to "open any browser, anywhere."

The creatures move. The ecosystem lives. The web has Terrarium.

---

## The Scorecard

| What Broke | Why Tests Didn't Catch It | Time to Fix |
|-----------|--------------------------|-------------|
| JS interop on wrong thread | Tests don't exercise Blazor circuit threading | 1 line (InvokeAsync wrapper) |
| Sprites never preloaded | Tests mock the renderer | 4 lines (preloadAll call) |
| BMP magenta not stripped | No visual regression tests | 8 lines (pixel scan loop) |
| Viewport at wrong position | Tests don't check viewport coords | 5 lines (center on creatures) |

Four bugs. 18 lines of code total. The difference between "nothing works" and "everything works."

---

## What's Next

The ecosystem is alive. The creatures move. The next question is obvious:

*Can I build my own?*

Yes. The `Terrarium.Samples` project has three creature implementations ready to study: `SimplePlant`, `SimpleHerbivore`, and `SimpleCarnivore`. Each one shows you how to wire up the event-driven lifecycle, distribute your 100 attribute points, and build a survival strategy.

The SDK is waiting. The ecosystem is hungry.

Bring your creatures.
