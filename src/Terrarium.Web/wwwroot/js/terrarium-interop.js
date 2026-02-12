/**
 * Terrarium Blazor JS interop module.
 * Provides Canvas initialization, resize handling, sprite loading, and frame rendering.
 *
 * Depends on (loaded via <script> tags before this module):
 *   - terrarium-sprites.js  (SpriteSheet, SpriteAnimation classes)
 *   - sprite-loader.js      (low-level BMP loading)
 *   - sprite-manager.js     (high-level sprite cache & draw API)
 */

let _canvas = null;
let _ctx = null;
let _resizeObserver = null;
let _spritesReady = false;
let _lastFrameTime = 0;

/**
 * Initializes the Canvas 2D rendering context and sets up resize handling.
 * @param {HTMLCanvasElement} canvasElement - The canvas element reference from Blazor.
 * @returns {Promise<{ width: number, height: number }>} The initial canvas dimensions.
 */
export async function initializeCanvas(canvasElement) {
    _canvas = canvasElement;
    _ctx = _canvas.getContext('2d');

    resizeCanvas();

    _resizeObserver = new ResizeObserver(() => resizeCanvas());
    _resizeObserver.observe(_canvas.parentElement);

    window.addEventListener('resize', resizeCanvas);

    // Load sprite manifest in the background
    try {
        await SpriteManager.loadManifest();
    } catch (e) {
        console.warn('Sprite manifest load deferred:', e.message);
    }

    return { width: _canvas.width, height: _canvas.height };
}

/**
 * Resizes the canvas to match its parent container dimensions.
 * @returns {{ width: number, height: number }} The new canvas dimensions.
 */
export function resizeCanvas() {
    if (!_canvas || !_canvas.parentElement) return { width: 0, height: 0 };

    const parent = _canvas.parentElement;
    _canvas.width = parent.clientWidth;
    _canvas.height = parent.clientHeight;

    return { width: _canvas.width, height: _canvas.height };
}

/**
 * Preloads sprite sheets so they're ready for rendering.
 * @param {string[]} [spriteIds] - Specific IDs to load, or omit for all.
 * @param {boolean} [useLarge=true] - Load 48px (true) or 24px (false).
 * @returns {Promise<string[]>} The IDs that were loaded.
 */
export async function loadSprites(spriteIds, useLarge) {
    if (spriteIds && spriteIds.length > 0) {
        await Promise.all(spriteIds.map(id => SpriteManager.loadSpriteSheet(id, useLarge)));
    } else {
        await SpriteManager.preloadAll(useLarge);
    }
    _spritesReady = true;
    return SpriteManager.getSpriteIds();
}

/**
 * Draws a single sprite at a given position (stateless, frame-indexed).
 * Called from Blazor for individual entity rendering.
 * @param {string} spriteId - Creature key (e.g., 'ant', 'plant', 'teleporter').
 * @param {string} action - Action name ('attacked','defended','died','ate','moved','idle').
 * @param {string} direction - Direction ('n','ne','e','se','s','sw','w','nw').
 * @param {number} frameIndex - Frame within the animation.
 * @param {number} x - Canvas X.
 * @param {number} y - Canvas Y.
 * @param {number} [size] - Draw size override.
 * @returns {boolean} True if drawn.
 */
export function drawSprite(spriteId, action, direction, frameIndex, x, y, size) {
    if (!_ctx) return false;
    return SpriteManager.drawSprite(_ctx, spriteId, action, direction, frameIndex, x, y, size);
}

/**
 * Renders a single frame of the world state to the canvas.
 * worldState.entities is an array of:
 *   { id, spriteId, action, direction, frameIndex, x, y, size? }
 * @param {object} worldState - The serialized world state from the server.
 */
export function drawFrame(worldState) {
    if (!_ctx || !_canvas) return;

    const now = performance.now();
    const deltaMs = _lastFrameTime ? now - _lastFrameTime : 16;
    _lastFrameTime = now;

    _ctx.clearRect(0, 0, _canvas.width, _canvas.height);

    if (!worldState || !worldState.entities) return;

    for (const entity of worldState.entities) {
        if (entity.animated) {
            SpriteManager.drawAnimated(
                _ctx, entity.spriteId, entity.action, entity.direction,
                deltaMs, entity.x, entity.y, entity.size
            );
        } else {
            SpriteManager.drawSprite(
                _ctx, entity.spriteId, entity.action, entity.direction,
                entity.frameIndex ?? 0, entity.x, entity.y, entity.size
            );
        }
    }
}

/**
 * Returns info about the sprite system state.
 * @returns {{ ready: boolean, loadedCount: number, spriteIds: string[] }}
 */
export function getSpriteStatus() {
    return {
        ready: _spritesReady,
        loadedCount: SpriteManager.getSpriteIds().length,
        spriteIds: SpriteManager.getSpriteIds()
    };
}

/**
 * Cleans up canvas resources, sprite caches, and event listeners.
 */
export function dispose() {
    if (_resizeObserver) {
        _resizeObserver.disconnect();
        _resizeObserver = null;
    }
    window.removeEventListener('resize', resizeCanvas);
    SpriteManager.dispose();
    _spritesReady = false;
    _lastFrameTime = 0;
    _canvas = null;
    _ctx = null;
}
