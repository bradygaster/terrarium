/**
 * Terrarium Blazor JS interop module.
 * Provides Canvas initialization, resize handling, and frame rendering stubs.
 */

let _canvas = null;
let _ctx = null;
let _resizeObserver = null;

/**
 * Initializes the Canvas 2D rendering context and sets up resize handling.
 * @param {HTMLCanvasElement} canvasElement - The canvas element reference from Blazor.
 * @returns {{ width: number, height: number }} The initial canvas dimensions.
 */
export function initializeCanvas(canvasElement) {
    _canvas = canvasElement;
    _ctx = _canvas.getContext('2d');

    resizeCanvas();

    _resizeObserver = new ResizeObserver(() => resizeCanvas());
    _resizeObserver.observe(_canvas.parentElement);

    window.addEventListener('resize', resizeCanvas);

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
 * Renders a single frame of the world state to the canvas.
 * @param {object} worldState - The serialized world state from the server.
 */
export function drawFrame(worldState) {
    if (!_ctx || !_canvas) return;

    _ctx.clearRect(0, 0, _canvas.width, _canvas.height);

    // TODO: Render creatures, terrain, and other world elements
    // using data from worldState and the SpriteLoader module.
}

/**
 * Cleans up canvas resources and event listeners.
 */
export function dispose() {
    if (_resizeObserver) {
        _resizeObserver.disconnect();
        _resizeObserver = null;
    }
    window.removeEventListener('resize', resizeCanvas);
    _canvas = null;
    _ctx = null;
}
