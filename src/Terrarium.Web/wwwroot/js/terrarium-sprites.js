/**
 * Terrarium Sprite System — SpriteSheet, SpriteAnimation, and direction mapping.
 * Works with the 10-column × 40-row animal sprite sheets, single-frame plants,
 * and 16-frame teleporter from the original .NET Terrarium.
 *
 * Layout (animals):
 *   Rows  0- 7: attacked (8 directions)
 *   Rows  8-15: defended
 *   Rows 16-23: died
 *   Rows 24-31: ate
 *   Rows 32-39: moved
 *   Each row: 10 frames, left-to-right
 *   Direction order per action block: E, SE, S, SW, W, NW, N, NE (rows 0-7)
 */

// ─── Direction Mapping ──────────────────────────────────────────────
const Directions = Object.freeze({
    E:  0, SE: 1, S:  2, SW: 3,
    W:  4, NW: 5, N:  6, NE: 7
});

const DIRECTION_NAMES = Object.freeze(['e', 'se', 's', 'sw', 'w', 'nw', 'n', 'ne']);

/**
 * Resolves a direction value to a 0-based row offset.
 * Accepts number (0-7), string name, or radian angle.
 * @param {number|string} dir - Direction identifier.
 * @returns {number} Row offset within an action block (0-7).
 */
function resolveDirection(dir) {
    if (typeof dir === 'number' && dir >= 0 && dir <= 7) return dir;
    if (typeof dir === 'string') {
        const upper = dir.toUpperCase();
        if (upper in Directions) return Directions[upper];
        // Also accept lowercase keys from animations.json
        const idx = DIRECTION_NAMES.indexOf(dir.toLowerCase());
        if (idx !== -1) return idx;
    }
    return Directions.S; // default facing south
}

// ─── Action Mapping ─────────────────────────────────────────────────
const Actions = Object.freeze({
    ATTACKED: 'attacked',
    DEFENDED: 'defended',
    DIED:     'died',
    ATE:      'ate',
    MOVED:    'moved'
});

const ACTION_BASE_ROWS = Object.freeze({
    attacked:  0,
    defended:  8,
    died:     16,
    ate:      24,
    moved:    32
});

// ─── SpriteSheet ────────────────────────────────────────────────────
/**
 * Represents a loaded sprite sheet image with known frame geometry.
 */
class SpriteSheet {
    /**
     * @param {ImageBitmap} bitmap - The loaded sprite image.
     * @param {number} frameSize - Pixel width/height of each frame (24 or 48).
     * @param {string} type - 'animal', 'plant', or 'effect'.
     * @param {number} columns - Frames per row (10 for animals, 16 for teleporter, 1 for plants).
     * @param {number} rows - Total rows in the sheet.
     */
    constructor(bitmap, frameSize, type, columns, rows) {
        this.bitmap = bitmap;
        this.frameSize = frameSize;
        this.type = type;
        this.columns = columns;
        this.rows = rows;
    }

    /**
     * Draws a single frame from this sheet.
     * @param {CanvasRenderingContext2D} ctx
     * @param {number} col - Column index (0-based).
     * @param {number} row - Row index (0-based).
     * @param {number} destX - Canvas X.
     * @param {number} destY - Canvas Y.
     * @param {number} [destSize] - Output size (defaults to frameSize).
     */
    drawFrame(ctx, col, row, destX, destY, destSize) {
        const fs = this.frameSize;
        const ds = destSize ?? fs;
        ctx.drawImage(
            this.bitmap,
            col * fs, row * fs, fs, fs,
            destX, destY, ds, ds
        );
    }

    /** Release the underlying ImageBitmap. */
    dispose() {
        if (this.bitmap) {
            this.bitmap.close();
            this.bitmap = null;
        }
    }
}

// ─── SpriteAnimation ────────────────────────────────────────────────
/**
 * Manages frame sequencing for a sprite animation.
 */
class SpriteAnimation {
    /**
     * @param {SpriteSheet} sheet - The sprite sheet to animate from.
     * @param {number} row - Sheet row for this animation sequence.
     * @param {number} startFrame - First frame column index.
     * @param {number} frameCount - Number of frames in the sequence.
     * @param {number} frameDuration - Milliseconds per frame.
     * @param {boolean} loop - Whether the animation loops.
     */
    constructor(sheet, row, startFrame, frameCount, frameDuration, loop) {
        this.sheet = sheet;
        this.row = row;
        this.startFrame = startFrame;
        this.frameCount = frameCount;
        this.frameDuration = frameDuration;
        this.loop = loop;
        this._elapsed = 0;
        this._currentFrame = 0;
        this._finished = false;
    }

    /** Reset the animation to its first frame. */
    reset() {
        this._elapsed = 0;
        this._currentFrame = 0;
        this._finished = false;
    }

    /** @returns {boolean} True if non-looping animation has completed. */
    get finished() { return this._finished; }

    /** @returns {number} Current frame index within the sequence. */
    get currentFrame() { return this._currentFrame; }

    /**
     * Advance the animation by a time delta.
     * @param {number} deltaMs - Milliseconds since last update.
     */
    update(deltaMs) {
        if (this._finished) return;
        this._elapsed += deltaMs;
        const totalFrameTime = this.frameDuration * this.frameCount;

        if (this.loop) {
            this._currentFrame = Math.floor(
                (this._elapsed / this.frameDuration) % this.frameCount
            );
        } else {
            if (this._elapsed >= totalFrameTime) {
                this._currentFrame = this.frameCount - 1;
                this._finished = true;
            } else {
                this._currentFrame = Math.floor(this._elapsed / this.frameDuration);
            }
        }
    }

    /**
     * Draw the current frame to the canvas.
     * @param {CanvasRenderingContext2D} ctx
     * @param {number} destX
     * @param {number} destY
     * @param {number} [destSize]
     */
    draw(ctx, destX, destY, destSize) {
        const col = this.startFrame + this._currentFrame;
        this.sheet.drawFrame(ctx, col, this.row, destX, destY, destSize);
    }

    /**
     * Draw a specific frame index (stateless, no timing).
     * @param {CanvasRenderingContext2D} ctx
     * @param {number} frameIndex - Frame within the sequence (0-based).
     * @param {number} destX
     * @param {number} destY
     * @param {number} [destSize]
     */
    drawAtFrame(ctx, frameIndex, destX, destY, destSize) {
        const col = this.startFrame + (frameIndex % this.frameCount);
        this.sheet.drawFrame(ctx, col, this.row, destX, destY, destSize);
    }
}

// ─── Factory helpers ────────────────────────────────────────────────

/**
 * Creates a SpriteAnimation for an animal action + direction.
 * @param {SpriteSheet} sheet - An animal sprite sheet.
 * @param {string} action - One of: attacked, defended, died, ate, moved.
 * @param {number|string} direction - Direction (see resolveDirection).
 * @param {object} [opts] - Override frameDuration, frameCount, loop.
 * @returns {SpriteAnimation}
 */
function createAnimalAnimation(sheet, action, direction, opts) {
    const baseRow = ACTION_BASE_ROWS[action];
    if (baseRow === undefined) {
        throw new Error(`Unknown action: ${action}`);
    }
    const dirOffset = resolveDirection(direction);
    const row = baseRow + dirOffset;
    const frameCount = opts?.frameCount ?? 10;
    const frameDuration = opts?.frameDuration ?? 80;
    const loop = opts?.loop ?? false;
    return new SpriteAnimation(sheet, row, 0, frameCount, frameDuration, loop);
}

/**
 * Creates a SpriteAnimation for a plant (single static frame).
 * @param {SpriteSheet} sheet - A plant sprite sheet.
 * @returns {SpriteAnimation}
 */
function createPlantAnimation(sheet) {
    return new SpriteAnimation(sheet, 0, 0, 1, 1000, false);
}

/**
 * Creates a SpriteAnimation for the teleporter effect (16 frames, looping).
 * @param {SpriteSheet} sheet - The teleporter sprite sheet.
 * @returns {SpriteAnimation}
 */
function createTeleporterAnimation(sheet) {
    return new SpriteAnimation(sheet, 0, 0, 16, 60, true);
}

// ─── Exports ────────────────────────────────────────────────────────
const TerrariumSprites = {
    Directions,
    DIRECTION_NAMES,
    resolveDirection,
    Actions,
    ACTION_BASE_ROWS,
    SpriteSheet,
    SpriteAnimation,
    createAnimalAnimation,
    createPlantAnimation,
    createTeleporterAnimation
};

if (typeof module !== 'undefined' && module.exports) {
    module.exports = TerrariumSprites;
}
