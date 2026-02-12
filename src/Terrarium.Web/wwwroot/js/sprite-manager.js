/**
 * Terrarium Sprite Manager — loads, caches, and draws sprites for the game renderer.
 * Consumes animations.json manifest and provides a single drawSprite() entry point.
 *
 * Depends on:
 *   - terrarium-sprites.js  (SpriteSheet, SpriteAnimation, direction helpers)
 *   - sprite-loader.js      (low-level BMP loading via SpriteLoader)
 */

const SpriteManager = (() => {
    /** @type {object|null} Parsed animations.json metadata */
    let _metadata = null;

    /** @type {Map<string, SpriteSheet>} spriteId → loaded SpriteSheet */
    const _sheets = new Map();

    /** @type {Map<string, SpriteAnimation>} cacheKey → active SpriteAnimation */
    const _animationCache = new Map();

    const SPRITE_BASE_PATH = '/assets/sprites/';
    const METADATA_URL = SPRITE_BASE_PATH + 'animations.json';

    // ─── Initialization ─────────────────────────────────────────────

    /**
     * Loads the animation metadata manifest.
     * @returns {Promise<object>} The parsed manifest.
     */
    async function loadManifest() {
        if (_metadata) return _metadata;
        const resp = await fetch(METADATA_URL);
        if (!resp.ok) throw new Error(`Failed to load manifest: ${resp.status}`);
        _metadata = await resp.json();
        return _metadata;
    }

    /**
     * Loads a single sprite sheet by creature/effect id.
     * @param {string} spriteId - Key in manifest creatures map (e.g., 'ant', 'plant', 'teleporter').
     * @param {boolean} [useLarge=true] - Load 48px (true) or 24px (false) sheet.
     * @returns {Promise<SpriteSheet>} The loaded SpriteSheet.
     */
    async function loadSpriteSheet(spriteId, useLarge) {
        const large = useLarge !== false;
        const cacheKey = spriteId + (large ? '_lg' : '_sm');

        if (_sheets.has(cacheKey)) return _sheets.get(cacheKey);

        if (!_metadata) await loadManifest();

        const entry = _metadata.creatures[spriteId];
        if (!entry) throw new Error(`Unknown sprite: ${spriteId}`);

        const sizeKey = large ? 'large' : 'small';
        const file = entry.spriteSheet[sizeKey];
        if (!file) throw new Error(`No ${sizeKey} sheet for: ${spriteId}`);

        const bitmap = await SpriteLoader.loadSheet(SPRITE_BASE_PATH + file);
        const sheetSize = entry.sheetSize[sizeKey];
        const frameSize = large
            ? (_metadata.frameSize?.large ?? 48)
            : (_metadata.frameSize?.small ?? 24);

        // Determine grid dimensions
        let columns, rows;
        if (entry.type === 'plant') {
            columns = 1;
            rows = 1;
        } else if (entry.type === 'effect') {
            // Teleporter: 16 frames in a single row
            columns = Math.round(sheetSize.width / frameSize);
            rows = Math.round(sheetSize.height / frameSize);
        } else {
            // Animal: 10 columns × 40 rows
            columns = _metadata.layout.framesPerRow;
            rows = _metadata.layout.totalRows;
        }

        const sheet = new TerrariumSprites.SpriteSheet(
            bitmap, frameSize, entry.type, columns, rows
        );
        _sheets.set(cacheKey, sheet);
        return sheet;
    }

    /**
     * Preloads all sprite sheets defined in the manifest.
     * @param {boolean} [useLarge=true]
     * @returns {Promise<void>}
     */
    async function preloadAll(useLarge) {
        if (!_metadata) await loadManifest();
        const ids = Object.keys(_metadata.creatures);
        await Promise.all(ids.map(id => loadSpriteSheet(id, useLarge)));
    }

    // ─── Drawing ────────────────────────────────────────────────────

    /**
     * High-level draw call for the renderer.
     *
     * @param {CanvasRenderingContext2D} ctx - Canvas context.
     * @param {string} spriteId - Creature/effect key (e.g., 'ant', 'spider', 'plant', 'teleporter').
     * @param {string} action - Animation action ('attacked','defended','died','ate','moved','idle').
     * @param {number|string} direction - Direction (0-7, or 'n','ne','e', etc.). Ignored for plants/teleporter.
     * @param {number} frameIndex - Frame within the animation sequence (0-based).
     * @param {number} x - Destination X on canvas.
     * @param {number} y - Destination Y on canvas.
     * @param {number} [size] - Destination draw size (defaults to frame size).
     * @returns {boolean} True if drawn successfully, false if sheet not loaded.
     */
    function drawSprite(ctx, spriteId, action, direction, frameIndex, x, y, size) {
        const cacheKey = spriteId + '_lg'; // default to large
        const sheet = _sheets.get(cacheKey) ?? _sheets.get(spriteId + '_sm');
        if (!sheet) return false;

        if (sheet.type === 'plant') {
            // Single-frame static sprite
            sheet.drawFrame(ctx, 0, 0, x, y, size);
            return true;
        }

        if (sheet.type === 'effect') {
            // Teleporter: cycle through 16 frames on row 0
            const col = frameIndex % sheet.columns;
            sheet.drawFrame(ctx, col, 0, x, y, size);
            return true;
        }

        // Animal sprite: look up row from action + direction
        const baseRow = TerrariumSprites.ACTION_BASE_ROWS[action];
        if (baseRow === undefined) {
            // Fallback: treat as idle (moved facing south, frame 0)
            const fallbackRow = TerrariumSprites.ACTION_BASE_ROWS.moved +
                TerrariumSprites.resolveDirection('s');
            sheet.drawFrame(ctx, 0, fallbackRow, x, y, size);
            return true;
        }

        const dirOffset = TerrariumSprites.resolveDirection(direction);
        const row = baseRow + dirOffset;
        const col = frameIndex % sheet.columns;
        sheet.drawFrame(ctx, col, row, x, y, size);
        return true;
    }

    /**
     * Gets or creates a SpriteAnimation for time-based animation.
     * Returns a cached instance keyed by spriteId+action+direction so
     * the same creature keeps its animation state across frames.
     *
     * @param {string} spriteId
     * @param {string} action
     * @param {number|string} direction
     * @returns {SpriteAnimation|null}
     */
    function getAnimation(spriteId, action, direction) {
        const sheet = _sheets.get(spriteId + '_lg') ?? _sheets.get(spriteId + '_sm');
        if (!sheet) return null;

        const dirKey = typeof direction === 'number' ? direction : TerrariumSprites.resolveDirection(direction);
        const key = `${spriteId}:${action}:${dirKey}`;

        if (_animationCache.has(key)) return _animationCache.get(key);

        let anim;
        if (sheet.type === 'plant') {
            anim = TerrariumSprites.createPlantAnimation(sheet);
        } else if (sheet.type === 'effect') {
            anim = TerrariumSprites.createTeleporterAnimation(sheet);
        } else {
            anim = TerrariumSprites.createAnimalAnimation(sheet, action, direction);
        }
        _animationCache.set(key, anim);
        return anim;
    }

    /**
     * Draws using time-based animation (update + draw in one call).
     * @param {CanvasRenderingContext2D} ctx
     * @param {string} spriteId
     * @param {string} action
     * @param {number|string} direction
     * @param {number} deltaMs - Time since last frame in ms.
     * @param {number} x
     * @param {number} y
     * @param {number} [size]
     * @returns {boolean}
     */
    function drawAnimated(ctx, spriteId, action, direction, deltaMs, x, y, size) {
        const anim = getAnimation(spriteId, action, direction);
        if (!anim) return false;
        anim.update(deltaMs);
        anim.draw(ctx, x, y, size);
        return true;
    }

    /**
     * Returns info about a loaded sprite.
     * @param {string} spriteId
     * @returns {{ type: string, frameSize: number, columns: number, rows: number }|null}
     */
    function getSpriteInfo(spriteId) {
        const sheet = _sheets.get(spriteId + '_lg') ?? _sheets.get(spriteId + '_sm');
        if (!sheet) return null;
        return {
            type: sheet.type,
            frameSize: sheet.frameSize,
            columns: sheet.columns,
            rows: sheet.rows
        };
    }

    /**
     * Returns all creature IDs defined in the manifest.
     * @returns {string[]}
     */
    function getSpriteIds() {
        if (!_metadata) return [];
        return Object.keys(_metadata.creatures);
    }

    /**
     * Resets an animation's state so it replays from frame 0.
     * @param {string} spriteId
     * @param {string} action
     * @param {number|string} direction
     */
    function resetAnimation(spriteId, action, direction) {
        const dirKey = typeof direction === 'number' ? direction : TerrariumSprites.resolveDirection(direction);
        const key = `${spriteId}:${action}:${dirKey}`;
        const anim = _animationCache.get(key);
        if (anim) anim.reset();
    }

    /**
     * Clears all cached sheets and animations, releasing GPU/memory resources.
     */
    function dispose() {
        for (const sheet of _sheets.values()) {
            sheet.dispose();
        }
        _sheets.clear();
        _animationCache.clear();
        _metadata = null;
        SpriteLoader.clearCache();
    }

    return {
        loadManifest,
        loadSpriteSheet,
        preloadAll,
        drawSprite,
        drawAnimated,
        getAnimation,
        resetAnimation,
        getSpriteInfo,
        getSpriteIds,
        dispose
    };
})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = SpriteManager;
}
