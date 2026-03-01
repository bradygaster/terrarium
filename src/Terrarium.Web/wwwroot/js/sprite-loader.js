/**
 * Terrarium sprite sheet loader and Canvas renderer.
 * Loads BMP sprite sheets and draws individual animation frames to HTML5 Canvas.
 */

const SpriteLoader = (() => {
    const _cache = new Map();

    /**
     * Loads a BMP sprite sheet, replaces the magenta (#FF00FF) transparency
     * key with actual alpha transparency, and caches the result.
     * @param {string} url - URL of the BMP sprite sheet.
     * @returns {Promise<ImageBitmap>} The loaded image bitmap with transparency applied.
     */
    async function loadSheet(url) {
        if (_cache.has(url)) {
            return _cache.get(url);
        }

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to load sprite sheet: ${url} (${response.status})`);
        }

        const blob = await response.blob();
        const rawBitmap = await createImageBitmap(blob);

        // Draw onto an offscreen canvas to access pixel data
        const offscreen = document.createElement('canvas');
        offscreen.width = rawBitmap.width;
        offscreen.height = rawBitmap.height;
        const ctx = offscreen.getContext('2d');
        ctx.drawImage(rawBitmap, 0, 0);
        rawBitmap.close();

        // Replace magenta (255, 0, 255) pixels with fully transparent
        const imageData = ctx.getImageData(0, 0, offscreen.width, offscreen.height);
        const data = imageData.data;
        for (let i = 0; i < data.length; i += 4) {
            if (data[i] === 255 && data[i + 1] === 0 && data[i + 2] === 255) {
                data[i + 3] = 0; // set alpha to transparent
            }
        }
        ctx.putImageData(imageData, 0, 0);

        const bitmap = await createImageBitmap(offscreen);
        _cache.set(url, bitmap);
        return bitmap;
    }

    /**
     * Draws a specific frame from a sprite sheet onto a Canvas context.
     * @param {CanvasRenderingContext2D} ctx - The canvas 2D rendering context.
     * @param {ImageBitmap} sheet - The loaded sprite sheet bitmap.
     * @param {number} frameX - Column index of the frame (0-based).
     * @param {number} frameY - Row index of the frame (0-based).
     * @param {number} frameSize - Width and height of each frame in pixels.
     * @param {number} destX - Destination X coordinate on the canvas.
     * @param {number} destY - Destination Y coordinate on the canvas.
     * @param {number} [destSize] - Destination draw size (defaults to frameSize).
     */
    function drawFrame(ctx, sheet, frameX, frameY, frameSize, destX, destY, destSize) {
        const sx = frameX * frameSize;
        const sy = frameY * frameSize;
        const ds = destSize ?? frameSize;

        ctx.drawImage(sheet, sx, sy, frameSize, frameSize, destX, destY, ds, ds);
    }

    /**
     * Draws an animation frame by action key using animation metadata.
     * @param {CanvasRenderingContext2D} ctx - The canvas 2D rendering context.
     * @param {ImageBitmap} sheet - The loaded sprite sheet bitmap.
     * @param {object} animation - Animation sequence from animations.json.
     * @param {number} frameIndex - Zero-based frame index within the animation.
     * @param {number} frameSize - Pixel size of each frame (24 or 48).
     * @param {number} destX - Destination X coordinate on the canvas.
     * @param {number} destY - Destination Y coordinate on the canvas.
     * @param {number} [destSize] - Destination draw size (defaults to frameSize).
     */
    function drawAnimationFrame(ctx, sheet, animation, frameIndex, frameSize, destX, destY, destSize) {
        const frame = animation.startFrame + (frameIndex % animation.frameCount);
        drawFrame(ctx, sheet, frame, animation.row, frameSize, destX, destY, destSize);
    }

    /**
     * Loads the animations.json metadata file.
     * @param {string} [url='/assets/sprites/animations.json'] - URL of the metadata file.
     * @returns {Promise<object>} The parsed animation metadata.
     */
    async function loadMetadata(url) {
        const metadataUrl = url ?? '/assets/sprites/animations.json';
        const response = await fetch(metadataUrl);
        if (!response.ok) {
            throw new Error(`Failed to load sprite metadata: ${metadataUrl} (${response.status})`);
        }
        return response.json();
    }

    /**
     * Preloads all sprite sheets for a set of creature families.
     * @param {object} metadata - The animation metadata object.
     * @param {string[]} families - Array of creature family names to preload.
     * @param {boolean} [useLarge=true] - Whether to load large (48px) or small (24px) sheets.
     * @param {string} [basePath='/assets/sprites/'] - Base URL path for sprite sheets.
     * @returns {Promise<Map<string, ImageBitmap>>} Map of family name to loaded bitmap.
     */
    async function preloadSheets(metadata, families, useLarge, basePath) {
        const base = basePath ?? '/assets/sprites/';
        const large = useLarge ?? true;
        const sheets = new Map();

        const promises = families.map(async (family) => {
            const creature = metadata.creatures[family];
            if (!creature) return;

            const file = large ? creature.spriteSheet.large : creature.spriteSheet.small;
            if (!file) return;

            const bitmap = await loadSheet(base + file);
            sheets.set(family, bitmap);
        });

        await Promise.all(promises);
        return sheets;
    }

    /**
     * Clears the sprite sheet cache, releasing ImageBitmap resources.
     */
    function clearCache() {
        for (const bitmap of _cache.values()) {
            bitmap.close();
        }
        _cache.clear();
    }

    return {
        loadSheet,
        drawFrame,
        drawAnimationFrame,
        loadMetadata,
        preloadSheets,
        clearCache
    };
})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = SpriteLoader;
}
