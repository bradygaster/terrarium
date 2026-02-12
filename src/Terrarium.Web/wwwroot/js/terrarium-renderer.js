/**
 * Terrarium Canvas Rendering Engine
 * Handles terrain, sprites, text overlays, viewport, and user interaction.
 * Imported as an ES module by the GameView Blazor component.
 */

// ---------------------------------------------------------------------------
// Viewport state
// ---------------------------------------------------------------------------
let _canvas = null;
let _ctx = null;
let _resizeObserver = null;
let _dotNetRef = null;

const _viewport = {
    x: 0,
    y: 0,
    zoom: 1.0,
    worldWidth: 0,
    worldHeight: 0
};

// Terrain tile cache
const _terrainTiles = {
    background: null,
    dirt: null
};

// Interaction state
const _interaction = {
    isDragging: false,
    dragStartX: 0,
    dragStartY: 0,
    viewportStartX: 0,
    viewportStartY: 0,
    hoveredCreature: null,
    selectedCreatureId: null
};

// Tooltip state
const _tooltip = {
    visible: false,
    x: 0,
    y: 0,
    text: ''
};

// Teleportation effect state
const _teleportation = {
    zones: [], // { x, y, width, height, edge } - portal zones at world edges
    activePortals: [], // { x, y, frameIndex, type: 'arrival'|'departure', timestamp }
    notifications: [], // { text, timestamp } - toast notifications
    activityLog: [] // { message, timestamp } - activity log entries
};

// Teleporter sprite sheet (16 frames × 1 row)
let _teleporterSheet = null;

// Last rendered world state for hit-testing
let _lastWorldState = null;

// Performance tracking state
const _performance = {
    frameTimes: [],
    maxSamples: 60, // Track last 60 frames (2 seconds at 30fps)
    lastFrameStart: 0,
    totalFrames: 0,
    stats: {
        avgFrameTime: 0,
        minFrameTime: 0,
        maxFrameTime: 0,
        fps: 0,
        framesOver33ms: 0 // Count of frames exceeding 30fps target
    }
};

// ---------------------------------------------------------------------------
// Initialization
// ---------------------------------------------------------------------------

/**
 * Initializes the renderer on a canvas element.
 * @param {HTMLCanvasElement} canvasElement
 * @param {object} dotNetRef - Blazor DotNetObjectReference for callbacks
 * @param {number} worldWidth - World width in pixels
 * @param {number} worldHeight - World height in pixels
 * @returns {{ width: number, height: number }}
 */
export async function initialize(canvasElement, dotNetRef, worldWidth, worldHeight) {
    _canvas = canvasElement;
    _ctx = _canvas.getContext('2d');
    _dotNetRef = dotNetRef;
    _viewport.worldWidth = worldWidth || 5000;
    _viewport.worldHeight = worldHeight || 5000;

    _fitCanvasToParent();

    _resizeObserver = new ResizeObserver(() => _fitCanvasToParent());
    _resizeObserver.observe(_canvas.parentElement);
    window.addEventListener('resize', _fitCanvasToParent);

    // Load terrain tiles
    await _loadTerrainTiles();

    // Load teleporter sprite
    await _loadTeleporterSprite();

    // Initialize teleport zones at world edges
    _initializeTeleportZones();

    // Bind interaction events
    _bindEvents();

    return { width: _canvas.width, height: _canvas.height };
}

// ---------------------------------------------------------------------------
// Resize
// ---------------------------------------------------------------------------

function _fitCanvasToParent() {
    if (!_canvas || !_canvas.parentElement) return;
    const parent = _canvas.parentElement;
    _canvas.width = parent.clientWidth;
    _canvas.height = parent.clientHeight;
}

/**
 * Resize the canvas to explicit dimensions.
 * @param {number} width
 * @param {number} height
 */
export function resize(width, height) {
    if (!_canvas) return;
    if (width && height) {
        _canvas.width = width;
        _canvas.height = height;
    } else {
        _fitCanvasToParent();
    }
}

// ---------------------------------------------------------------------------
// Terrain tile loading
// ---------------------------------------------------------------------------

async function _loadTerrainTiles() {
    const loadImage = (src) => new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = () => {
            console.warn(`Terrain tile not found: ${src}, using fallback`);
            resolve(null);
        };
        img.src = src;
    });

    const [bg, dirt] = await Promise.all([
        loadImage('/assets/terrain/background.bmp'),
        loadImage('/assets/terrain/dirt.bmp')
    ]);

    _terrainTiles.background = bg;
    _terrainTiles.dirt = dirt;
}

async function _loadTeleporterSprite() {
    const loadImage = (src) => new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = () => {
            console.warn(`Teleporter sprite not found: ${src}`);
            resolve(null);
        };
        img.src = src;
    });

    _teleporterSheet = await loadImage('/assets/sprites/teleporter.bmp');
}

function _initializeTeleportZones() {
    const zoneWidth = 100;
    const zoneHeight = 100;
    const ww = _viewport.worldWidth;
    const wh = _viewport.worldHeight;

    _teleportation.zones = [
        { x: 0, y: 0, width: zoneWidth, height: wh, edge: 'left' },
        { x: ww - zoneWidth, y: 0, width: zoneWidth, height: wh, edge: 'right' },
        { x: 0, y: 0, width: ww, height: zoneHeight, edge: 'top' },
        { x: 0, y: wh - zoneHeight, width: ww, height: zoneHeight, edge: 'bottom' }
    ];
}

// ---------------------------------------------------------------------------
// Clear
// ---------------------------------------------------------------------------

/**
 * Clears the entire canvas.
 */
export function clear() {
    if (!_ctx || !_canvas) return;
    _ctx.clearRect(0, 0, _canvas.width, _canvas.height);
}

// ---------------------------------------------------------------------------
// Terrain rendering (Issue #57)
// ---------------------------------------------------------------------------

const TILE_SIZE = 64;

/**
 * Draws the terrain grid across the visible viewport area.
 * @param {object} [terrainData] - Optional terrain data with tile type per cell.
 */
export function drawTerrain(terrainData) {
    if (!_ctx || !_canvas) return;

    const vx = _viewport.x;
    const vy = _viewport.y;
    const zoom = _viewport.zoom;
    const cw = _canvas.width;
    const ch = _canvas.height;

    // Calculate visible tile range
    const startCol = Math.floor(vx / TILE_SIZE);
    const startRow = Math.floor(vy / TILE_SIZE);
    const endCol = Math.ceil((vx + cw / zoom) / TILE_SIZE);
    const endRow = Math.ceil((vy + ch / zoom) / TILE_SIZE);

    _ctx.save();
    _ctx.scale(zoom, zoom);
    _ctx.translate(-vx, -vy);

    for (let row = startRow; row <= endRow; row++) {
        for (let col = startCol; col <= endCol; col++) {
            const px = col * TILE_SIZE;
            const py = row * TILE_SIZE;

            // Determine tile type
            const isDirt = terrainData
                ? terrainData[`${col},${row}`] === 'dirt'
                : false;

            const tile = isDirt ? _terrainTiles.dirt : _terrainTiles.background;

            if (tile) {
                _ctx.drawImage(tile, px, py, TILE_SIZE, TILE_SIZE);
            } else {
                // Fallback: solid color fill
                _ctx.fillStyle = isDirt ? '#8B6914' : '#2d5a27';
                _ctx.fillRect(px, py, TILE_SIZE, TILE_SIZE);
            }

            // Grid lines
            _ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
            _ctx.lineWidth = 0.5;
            _ctx.strokeRect(px, py, TILE_SIZE, TILE_SIZE);
        }
    }

    _ctx.restore();
}

// ---------------------------------------------------------------------------
// Sprite rendering
// ---------------------------------------------------------------------------

/**
 * Loads and caches sprite sheets via SpriteManager.
 * Call before rendering to ensure assets are ready.
 * @param {string[]} [spriteIds] - Specific IDs to load, or omit for all.
 * @param {boolean} [useLarge=true] - Load 48px (true) or 24px (false).
 * @returns {Promise<string[]>} Loaded sprite IDs.
 */
export async function loadSprites(spriteIds, useLarge) {
    if (typeof SpriteManager === 'undefined') {
        console.warn('SpriteManager not available — include terrarium-sprites.js, sprite-loader.js, and sprite-manager.js');
        return [];
    }
    if (spriteIds && spriteIds.length > 0) {
        await Promise.all(spriteIds.map(id => SpriteManager.loadSpriteSheet(id, useLarge)));
    } else {
        await SpriteManager.preloadAll(useLarge);
    }
    return SpriteManager.getSpriteIds();
}

/**
 * Draws a creature sprite at world coordinates using SpriteManager.
 * @param {string} spriteId - Creature key (e.g., 'ant', 'plant').
 * @param {string} action - Action name.
 * @param {string} direction - Direction name.
 * @param {number} frameIndex - Frame within the animation.
 * @param {number} worldX - World X.
 * @param {number} worldY - World Y.
 * @param {number} [size] - Draw size.
 * @returns {boolean} True if drawn.
 */
export function drawCreatureSprite(spriteId, action, direction, frameIndex, worldX, worldY, size) {
    if (!_ctx || typeof SpriteManager === 'undefined') return false;

    const zoom = _viewport.zoom;
    const screenX = (worldX - _viewport.x) * zoom;
    const screenY = (worldY - _viewport.y) * zoom;
    const drawSize = (size ?? 48) * zoom;

    return SpriteManager.drawSprite(_ctx, spriteId, action, direction, frameIndex, screenX, screenY, drawSize);
}

/**
 * Draws a sprite at world coordinates (raw source coordinates).
 * @param {HTMLImageElement|ImageBitmap} image - Sprite sheet image
 * @param {number} srcX - Source X in sprite sheet
 * @param {number} srcY - Source Y in sprite sheet
 * @param {number} srcW - Source width
 * @param {number} srcH - Source height
 * @param {number} worldX - Destination X in world coordinates
 * @param {number} worldY - Destination Y in world coordinates
 * @param {number} destW - Destination width
 * @param {number} destH - Destination height
 */
export function drawSprite(image, srcX, srcY, srcW, srcH, worldX, worldY, destW, destH) {
    if (!_ctx) return;

    const zoom = _viewport.zoom;
    const screenX = (worldX - _viewport.x) * zoom;
    const screenY = (worldY - _viewport.y) * zoom;

    _ctx.drawImage(image, srcX, srcY, srcW, srcH,
        screenX, screenY, destW * zoom, destH * zoom);
}

// ---------------------------------------------------------------------------
// Text overlays (Issue #58)
// ---------------------------------------------------------------------------

/**
 * Draws a text label at world coordinates.
 * @param {string} text - The text to draw
 * @param {number} worldX - X position in world coordinates
 * @param {number} worldY - Y position in world coordinates
 * @param {object} [options] - Rendering options
 * @param {string} [options.font] - CSS font string
 * @param {string} [options.fillStyle] - Text fill color
 * @param {string} [options.strokeStyle] - Text outline color
 * @param {string} [options.align] - Text alignment (center, left, right)
 * @param {string} [options.baseline] - Text baseline (top, middle, bottom)
 * @param {number} [options.maxWidth] - Maximum text width in pixels
 */
export function drawText(text, worldX, worldY, options) {
    if (!_ctx || !text) return;

    const zoom = _viewport.zoom;
    const screenX = (worldX - _viewport.x) * zoom;
    const screenY = (worldY - _viewport.y) * zoom;

    const opts = options || {};
    _ctx.save();
    _ctx.font = opts.font || `${Math.max(10, 12 * zoom)}px 'Segoe UI', Arial, sans-serif`;
    _ctx.textAlign = opts.align || 'center';
    _ctx.textBaseline = opts.baseline || 'bottom';

    // Outline for readability
    if (opts.strokeStyle !== 'none') {
        _ctx.strokeStyle = opts.strokeStyle || 'rgba(0, 0, 0, 0.8)';
        _ctx.lineWidth = 3;
        _ctx.lineJoin = 'round';
        _ctx.strokeText(text, screenX, screenY, opts.maxWidth);
    }

    _ctx.fillStyle = opts.fillStyle || '#ffffff';
    _ctx.fillText(text, screenX, screenY, opts.maxWidth);
    _ctx.restore();
}

/**
 * Draws a creature name label above its position.
 * @param {string} name - Creature name/species
 * @param {number} worldX - Creature center X
 * @param {number} worldY - Creature top Y
 * @param {boolean} [selected] - Whether the creature is selected
 */
export function drawCreatureLabel(name, worldX, worldY, selected) {
    drawText(name, worldX, worldY - 4, {
        fillStyle: selected ? '#00ffcc' : '#ffffff',
        font: `${selected ? 'bold ' : ''}${Math.max(10, 11 * _viewport.zoom)}px 'Segoe UI', Arial, sans-serif`
    });
}

/**
 * Draws a population count overlay at a fixed screen position.
 * @param {string} text - Population text (e.g., "Plants: 42 | Animals: 15")
 * @param {string} [position] - Position: 'top-left', 'top-right', 'bottom-left', 'bottom-right'
 */
export function drawStatusOverlay(text, position) {
    if (!_ctx || !_canvas || !text) return;

    const pos = position || 'top-left';
    const padding = 8;
    const margin = 12;

    _ctx.save();
    _ctx.font = '13px "Segoe UI", Arial, sans-serif';
    const metrics = _ctx.measureText(text);
    const textW = metrics.width;
    const textH = 16;

    let x, y;
    switch (pos) {
        case 'top-right':
            x = _canvas.width - textW - padding * 2 - margin;
            y = margin;
            break;
        case 'bottom-left':
            x = margin;
            y = _canvas.height - textH - padding * 2 - margin;
            break;
        case 'bottom-right':
            x = _canvas.width - textW - padding * 2 - margin;
            y = _canvas.height - textH - padding * 2 - margin;
            break;
        default: // top-left
            x = margin;
            y = margin;
            break;
    }

    // Background
    _ctx.fillStyle = 'rgba(0, 0, 0, 0.6)';
    _ctx.beginPath();
    _ctx.roundRect(x, y, textW + padding * 2, textH + padding * 2, 4);
    _ctx.fill();

    // Border
    _ctx.strokeStyle = 'rgba(255, 255, 255, 0.15)';
    _ctx.lineWidth = 1;
    _ctx.stroke();

    // Text
    _ctx.fillStyle = '#e0e0e0';
    _ctx.textAlign = 'left';
    _ctx.textBaseline = 'top';
    _ctx.fillText(text, x + padding, y + padding);
    _ctx.restore();
}

// ---------------------------------------------------------------------------
// Teleportation effects (Issue #74)
// ---------------------------------------------------------------------------

/**
 * Triggers a teleportation arrival animation at the specified world coordinates.
 * @param {number} worldX - Arrival X position in world coordinates
 * @param {number} worldY - Arrival Y position in world coordinates
 * @param {string} creatureName - Name of the arriving creature
 * @param {string} sourcePeerId - Source peer ID
 */
export function showTeleportArrival(worldX, worldY, creatureName, sourcePeerId) {
    _teleportation.activePortals.push({
        x: worldX,
        y: worldY,
        frameIndex: 0,
        type: 'arrival',
        timestamp: Date.now()
    });

    const message = `🌀 ${creatureName} arrived from ${sourcePeerId}`;
    _showNotification(message);
    _addActivityLog(message);
}

/**
 * Triggers a teleportation departure animation at the specified world coordinates.
 * @param {number} worldX - Departure X position in world coordinates
 * @param {number} worldY - Departure Y position in world coordinates
 * @param {string} creatureName - Name of the departing creature
 * @param {string} targetPeerId - Target peer ID (or "random peer")
 */
export function showTeleportDeparture(worldX, worldY, creatureName, targetPeerId) {
    _teleportation.activePortals.push({
        x: worldX,
        y: worldY,
        frameIndex: 0,
        type: 'departure',
        timestamp: Date.now()
    });

    const message = `🌀 ${creatureName} departed to ${targetPeerId}`;
    _showNotification(message);
    _addActivityLog(message);
}

/**
 * Returns the current teleportation activity log.
 * @returns {Array<{message: string, timestamp: number}>}
 */
export function getTeleportActivityLog() {
    return [..._teleportation.activityLog];
}

/**
 * Clears the teleportation activity log.
 */
export function clearTeleportActivityLog() {
    _teleportation.activityLog = [];
}

function _showNotification(text) {
    _teleportation.notifications.push({
        text,
        timestamp: Date.now()
    });

    // Auto-dismiss after 4 seconds
    setTimeout(() => {
        _teleportation.notifications = _teleportation.notifications.filter(n => n.text !== text);
    }, 4000);
}

function _addActivityLog(message) {
    _teleportation.activityLog.unshift({
        message,
        timestamp: Date.now()
    });

    // Keep only last 50 entries
    if (_teleportation.activityLog.length > 50) {
        _teleportation.activityLog = _teleportation.activityLog.slice(0, 50);
    }
}

function _updateTeleportEffects(deltaMs) {
    const now = Date.now();

    // Update active portal animations
    for (let i = _teleportation.activePortals.length - 1; i >= 0; i--) {
        const portal = _teleportation.activePortals[i];
        const elapsed = now - portal.timestamp;
        
        // 16 frames at 60ms each = 960ms total animation
        portal.frameIndex = Math.floor(elapsed / 60);
        
        // Remove finished animations
        if (portal.frameIndex >= 16) {
            _teleportation.activePortals.splice(i, 1);
        }
    }
}

function _drawTeleportZones() {
    if (!_ctx) return;

    _ctx.save();
    const zoom = _viewport.zoom;
    _ctx.scale(zoom, zoom);
    _ctx.translate(-_viewport.x, -_viewport.y);

    // Draw glowing borders at world edges
    for (const zone of _teleportation.zones) {
        const pulse = Math.sin(Date.now() / 500) * 0.3 + 0.7; // 0.4 to 1.0
        
        _ctx.strokeStyle = `rgba(128, 255, 255, ${pulse * 0.3})`;
        _ctx.lineWidth = 4;
        _ctx.setLineDash([8, 8]);
        _ctx.strokeRect(zone.x, zone.y, zone.width, zone.height);
        _ctx.setLineDash([]);

        // Inner glow
        _ctx.strokeStyle = `rgba(128, 255, 255, ${pulse * 0.5})`;
        _ctx.lineWidth = 2;
        _ctx.strokeRect(zone.x + 2, zone.y + 2, zone.width - 4, zone.height - 4);
    }

    _ctx.restore();
}

function _drawTeleportPortals() {
    if (!_ctx || !_teleporterSheet) return;

    const FRAME_SIZE = 48; // Teleporter sprite frame size
    const zoom = _viewport.zoom;

    for (const portal of _teleportation.activePortals) {
        const screenX = (portal.x - _viewport.x) * zoom;
        const screenY = (portal.y - _viewport.y) * zoom;
        const drawSize = FRAME_SIZE * zoom;

        // Clamp frame index
        const frame = Math.min(Math.max(portal.frameIndex, 0), 15);

        // Draw teleporter sprite frame
        _ctx.drawImage(
            _teleporterSheet,
            frame * FRAME_SIZE, 0, FRAME_SIZE, FRAME_SIZE,
            screenX, screenY, drawSize, drawSize
        );

        // Particle glow effect
        const alpha = portal.type === 'arrival' ? 0.6 : 0.4;
        const color = portal.type === 'arrival' ? '128, 255, 128' : '128, 128, 255';
        
        _ctx.save();
        _ctx.globalAlpha = alpha * (1 - frame / 16);
        _ctx.fillStyle = `rgba(${color}, 0.3)`;
        _ctx.beginPath();
        _ctx.arc(
            screenX + drawSize / 2,
            screenY + drawSize / 2,
            drawSize * (0.5 + frame / 32),
            0, Math.PI * 2
        );
        _ctx.fill();
        _ctx.restore();
    }
}

function _drawTeleportNotifications() {
    if (!_ctx || !_canvas) return;
    if (_teleportation.notifications.length === 0) return;

    const now = Date.now();
    const padding = 12;
    const lineHeight = 24;
    const margin = 16;
    let y = margin;

    _ctx.save();
    _ctx.font = '14px "Segoe UI", Arial, sans-serif';

    for (const notification of _teleportation.notifications) {
        const age = now - notification.timestamp;
        const fadeInDuration = 200;
        const fadeOutStart = 3500;
        const fadeOutDuration = 500;

        let alpha = 1.0;
        if (age < fadeInDuration) {
            alpha = age / fadeInDuration;
        } else if (age > fadeOutStart) {
            alpha = 1.0 - (age - fadeOutStart) / fadeOutDuration;
        }

        if (alpha <= 0) continue;

        const metrics = _ctx.measureText(notification.text);
        const w = metrics.width + padding * 2;
        const h = lineHeight + padding;
        const x = _canvas.width - w - margin;

        // Background with glass effect
        _ctx.globalAlpha = alpha * 0.95;
        _ctx.fillStyle = 'rgba(16, 16, 32, 0.95)';
        _ctx.beginPath();
        _ctx.roundRect(x, y, w, h, 6);
        _ctx.fill();

        // Border
        _ctx.strokeStyle = 'rgba(128, 255, 255, 0.5)';
        _ctx.lineWidth = 1;
        _ctx.stroke();

        // Text
        _ctx.globalAlpha = alpha;
        _ctx.fillStyle = '#00ffcc';
        _ctx.textAlign = 'left';
        _ctx.textBaseline = 'middle';
        _ctx.fillText(notification.text, x + padding, y + h / 2);

        y += h + 8;
    }

    _ctx.restore();
}

// ---------------------------------------------------------------------------
// World state rendering (full frame)
// ---------------------------------------------------------------------------

/**
 * Renders a complete frame from serialized world state.
 * @param {object} worldState - Serialized world state
 * @param {object[]} worldState.creatures - Array of creature render data
 * @param {object} [worldState.terrain] - Terrain tile data
 * @param {string} [worldState.statusText] - Status overlay text
 * @param {object} [spriteSheets] - Map of family name → loaded Image/ImageBitmap
 */
export function renderFrame(worldState, spriteSheets) {
    if (!_ctx || !_canvas) return;

    // Start performance timing
    const frameStart = performance.now();

    _lastWorldState = worldState;

    // 1. Clear
    clear();

    // 2. Terrain
    drawTerrain(worldState?.terrain);

    // 3. Creatures
    if (worldState?.creatures) {
        for (const creature of worldState.creatures) {
            let drawn = false;

            // Prefer SpriteManager for high-level rendering
            if (typeof SpriteManager !== 'undefined' && creature.skinFamily) {
                const action = creature.action || 'moved';
                const direction = creature.direction || 's';
                const frameIdx = creature.frameIndex ?? 0;
                const size = creature.frameSize || 48;
                drawn = drawCreatureSprite(
                    creature.skinFamily, action, direction, frameIdx,
                    creature.x, creature.y, size
                );
            }

            // Fallback to raw sprite sheet coordinates
            if (!drawn && spriteSheets) {
                const sheet = spriteSheets[creature.skinFamily];
                if (sheet) {
                    const frameSize = creature.frameSize || 48;
                    drawSprite(
                        sheet,
                        creature.srcX, creature.srcY, frameSize, frameSize,
                        creature.x, creature.y, frameSize, frameSize
                    );
                    drawn = true;
                }
            }

            if (drawn) {
                const frameSize = creature.frameSize || 48;

                // Selection highlight
                if (creature.id === _interaction.selectedCreatureId) {
                    _drawSelectionRing(creature.x, creature.y, frameSize);
                }

                // Name label
                if (creature.name) {
                    const isSelected = creature.id === _interaction.selectedCreatureId;
                    drawCreatureLabel(creature.name, creature.x + frameSize / 2, creature.y, isSelected);
                }
            }
        }
    }

    // 4. Teleport zones (glowing borders at world edges)
    _drawTeleportZones();

    // 5. Teleport portals (arrival/departure animations)
    _updateTeleportEffects(16); // Assume 60 FPS, ~16ms per frame
    _drawTeleportPortals();

    // 6. Status overlay
    if (worldState?.statusText) {
        drawStatusOverlay(worldState.statusText, 'top-left');
    }

    // 7. Tooltip
    if (_tooltip.visible) {
        _drawTooltip();
    }

    // 8. Teleport notifications (toast messages)
    _drawTeleportNotifications();

    // End performance timing and update stats
    const frameEnd = performance.now();
    const frameTime = frameEnd - frameStart;
    _updatePerformanceStats(frameTime);
}

function _drawSelectionRing(worldX, worldY, size) {
    const zoom = _viewport.zoom;
    const sx = (worldX - _viewport.x) * zoom;
    const sy = (worldY - _viewport.y) * zoom;
    const ss = size * zoom;

    _ctx.save();
    _ctx.strokeStyle = '#00ffcc';
    _ctx.lineWidth = 2;
    _ctx.setLineDash([4, 3]);
    _ctx.strokeRect(sx - 2, sy - 2, ss + 4, ss + 4);
    _ctx.setLineDash([]);
    _ctx.restore();
}

function _drawTooltip() {
    if (!_ctx || !_tooltip.visible) return;

    const padding = 6;
    _ctx.save();
    _ctx.font = '12px "Segoe UI", Arial, sans-serif';

    const lines = _tooltip.text.split('\n');
    let maxW = 0;
    for (const line of lines) {
        const m = _ctx.measureText(line);
        if (m.width > maxW) maxW = m.width;
    }
    const lineH = 16;
    const w = maxW + padding * 2;
    const h = lines.length * lineH + padding * 2;

    // Keep on-screen
    let tx = _tooltip.x + 14;
    let ty = _tooltip.y + 14;
    if (tx + w > _canvas.width) tx = _tooltip.x - w - 4;
    if (ty + h > _canvas.height) ty = _tooltip.y - h - 4;

    _ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    _ctx.beginPath();
    _ctx.roundRect(tx, ty, w, h, 4);
    _ctx.fill();

    _ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
    _ctx.lineWidth = 1;
    _ctx.stroke();

    _ctx.fillStyle = '#e0e0e0';
    _ctx.textAlign = 'left';
    _ctx.textBaseline = 'top';
    for (let i = 0; i < lines.length; i++) {
        _ctx.fillText(lines[i], tx + padding, ty + padding + i * lineH);
    }
    _ctx.restore();
}

// ---------------------------------------------------------------------------
// Viewport / pan / zoom (Issues #57, #59)
// ---------------------------------------------------------------------------

/**
 * Sets the viewport position in world coordinates.
 * @param {number} x
 * @param {number} y
 */
export function setViewport(x, y) {
    _viewport.x = _clampX(x);
    _viewport.y = _clampY(y);
}

/**
 * Pans the viewport by a delta in screen pixels.
 * @param {number} dx
 * @param {number} dy
 */
export function panViewport(dx, dy) {
    _viewport.x = _clampX(_viewport.x + dx / _viewport.zoom);
    _viewport.y = _clampY(_viewport.y + dy / _viewport.zoom);
}

/**
 * Sets the zoom level, clamped between 0.25 and 4.0.
 * @param {number} level
 */
export function setZoom(level) {
    _viewport.zoom = Math.max(0.25, Math.min(4.0, level));
}

/**
 * Gets the current viewport state.
 * @returns {{ x: number, y: number, zoom: number, canvasWidth: number, canvasHeight: number }}
 */
export function getViewport() {
    return {
        x: _viewport.x,
        y: _viewport.y,
        zoom: _viewport.zoom,
        canvasWidth: _canvas?.width || 0,
        canvasHeight: _canvas?.height || 0
    };
}

function _clampX(x) {
    const maxX = Math.max(0, _viewport.worldWidth - (_canvas?.width || 0) / _viewport.zoom);
    return Math.max(0, Math.min(maxX, x));
}

function _clampY(y) {
    const maxY = Math.max(0, _viewport.worldHeight - (_canvas?.height || 0) / _viewport.zoom);
    return Math.max(0, Math.min(maxY, y));
}

// ---------------------------------------------------------------------------
// Interaction events (Issue #59)
// ---------------------------------------------------------------------------

function _bindEvents() {
    if (!_canvas) return;

    _canvas.addEventListener('mousedown', _onMouseDown);
    _canvas.addEventListener('mousemove', _onMouseMove);
    _canvas.addEventListener('mouseup', _onMouseUp);
    _canvas.addEventListener('mouseleave', _onMouseLeave);
    _canvas.addEventListener('wheel', _onWheel, { passive: false });
    _canvas.addEventListener('contextmenu', (e) => e.preventDefault());

    // Keyboard events on the canvas's parent (needs focus)
    _canvas.tabIndex = 0;
    _canvas.addEventListener('keydown', _onKeyDown);
}

function _unbindEvents() {
    if (!_canvas) return;
    _canvas.removeEventListener('mousedown', _onMouseDown);
    _canvas.removeEventListener('mousemove', _onMouseMove);
    _canvas.removeEventListener('mouseup', _onMouseUp);
    _canvas.removeEventListener('mouseleave', _onMouseLeave);
    _canvas.removeEventListener('wheel', _onWheel);
    _canvas.removeEventListener('keydown', _onKeyDown);
}

function _onMouseDown(e) {
    if (e.button === 0 || e.button === 1) {
        _interaction.isDragging = true;
        _interaction.dragStartX = e.clientX;
        _interaction.dragStartY = e.clientY;
        _interaction.viewportStartX = _viewport.x;
        _interaction.viewportStartY = _viewport.y;
        _canvas.style.cursor = 'grabbing';
    }
}

function _onMouseMove(e) {
    const rect = _canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    if (_interaction.isDragging) {
        const dx = _interaction.dragStartX - e.clientX;
        const dy = _interaction.dragStartY - e.clientY;
        _viewport.x = _clampX(_interaction.viewportStartX + dx / _viewport.zoom);
        _viewport.y = _clampY(_interaction.viewportStartY + dy / _viewport.zoom);
        return;
    }

    // Hover hit-test
    _tooltip.visible = false;
    _canvas.style.cursor = 'default';

    if (_lastWorldState?.creatures) {
        const worldX = mx / _viewport.zoom + _viewport.x;
        const worldY = my / _viewport.zoom + _viewport.y;
        const hit = _hitTestCreature(worldX, worldY);

        if (hit) {
            _interaction.hoveredCreature = hit;
            _tooltip.visible = true;
            _tooltip.x = mx;
            _tooltip.y = my;
            _tooltip.text = hit.name || hit.skinFamily || 'Creature';
            if (hit.species) _tooltip.text += `\nSpecies: ${hit.species}`;
            if (hit.energy !== undefined) _tooltip.text += `\nEnergy: ${hit.energy}`;
            _canvas.style.cursor = 'pointer';
        } else {
            _interaction.hoveredCreature = null;
        }
    }
}

function _onMouseUp(e) {
    if (_interaction.isDragging) {
        const dx = Math.abs(e.clientX - _interaction.dragStartX);
        const dy = Math.abs(e.clientY - _interaction.dragStartY);

        // If barely moved, treat as a click
        if (dx < 4 && dy < 4) {
            _handleClick(e);
        }

        _interaction.isDragging = false;
        _canvas.style.cursor = 'default';
    }
}

function _onMouseLeave() {
    _interaction.isDragging = false;
    _tooltip.visible = false;
    _canvas.style.cursor = 'default';
}

function _onWheel(e) {
    e.preventDefault();
    const delta = e.deltaY > 0 ? -0.1 : 0.1;
    const oldZoom = _viewport.zoom;
    const newZoom = Math.max(0.25, Math.min(4.0, oldZoom + delta));

    // Zoom toward cursor position
    const rect = _canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    const worldX = mx / oldZoom + _viewport.x;
    const worldY = my / oldZoom + _viewport.y;

    _viewport.zoom = newZoom;
    _viewport.x = _clampX(worldX - mx / newZoom);
    _viewport.y = _clampY(worldY - my / newZoom);
}

function _onKeyDown(e) {
    const PAN_STEP = 50;
    let handled = true;

    switch (e.key) {
        case 'ArrowLeft':
        case 'a':
            panViewport(-PAN_STEP, 0);
            break;
        case 'ArrowRight':
        case 'd':
            panViewport(PAN_STEP, 0);
            break;
        case 'ArrowUp':
        case 'w':
            panViewport(0, -PAN_STEP);
            break;
        case 'ArrowDown':
        case 's':
            panViewport(0, PAN_STEP);
            break;
        case '+':
        case '=':
            setZoom(_viewport.zoom + 0.1);
            break;
        case '-':
            setZoom(_viewport.zoom - 0.1);
            break;
        case '0':
            // Reset viewport
            _viewport.x = 0;
            _viewport.y = 0;
            _viewport.zoom = 1.0;
            break;
        case 'Escape':
            _interaction.selectedCreatureId = null;
            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnCreatureDeselected_JS');
            }
            break;
        default:
            handled = false;
            break;
    }

    if (handled) {
        e.preventDefault();
    }
}

function _handleClick(e) {
    const rect = _canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    const worldX = mx / _viewport.zoom + _viewport.x;
    const worldY = my / _viewport.zoom + _viewport.y;

    const hit = _hitTestCreature(worldX, worldY);
    if (hit) {
        _interaction.selectedCreatureId = hit.id;
        if (_dotNetRef) {
            _dotNetRef.invokeMethodAsync('OnCreatureSelected_JS', hit.id, hit.name || '', hit.species || '');
        }
    } else {
        _interaction.selectedCreatureId = null;
        if (_dotNetRef) {
            _dotNetRef.invokeMethodAsync('OnCreatureDeselected_JS');
        }
    }
}

function _hitTestCreature(worldX, worldY) {
    if (!_lastWorldState?.creatures) return null;

    // Iterate in reverse (top-most first)
    for (let i = _lastWorldState.creatures.length - 1; i >= 0; i--) {
        const c = _lastWorldState.creatures[i];
        const size = c.frameSize || 48;
        if (worldX >= c.x && worldX <= c.x + size &&
            worldY >= c.y && worldY <= c.y + size) {
            return c;
        }
    }
    return null;
}

// ---------------------------------------------------------------------------
// Performance tracking
// ---------------------------------------------------------------------------

/**
 * Updates performance statistics with a new frame time measurement.
 * @param {number} frameTime - Frame render time in milliseconds
 */
function _updatePerformanceStats(frameTime) {
    _performance.frameTimes.push(frameTime);
    _performance.totalFrames++;

    // Keep only the last N samples
    if (_performance.frameTimes.length > _performance.maxSamples) {
        _performance.frameTimes.shift();
    }

    // Calculate statistics
    const times = _performance.frameTimes;
    const count = times.length;
    
    if (count > 0) {
        const sum = times.reduce((a, b) => a + b, 0);
        _performance.stats.avgFrameTime = sum / count;
        _performance.stats.minFrameTime = Math.min(...times);
        _performance.stats.maxFrameTime = Math.max(...times);
        _performance.stats.fps = 1000 / _performance.stats.avgFrameTime;
        _performance.stats.framesOver33ms = times.filter(t => t > 33).length;
    }
}

/**
 * Gets current performance statistics.
 * @returns {object} Performance stats
 */
export function getPerformanceStats() {
    return {
        ..._performance.stats,
        sampleCount: _performance.frameTimes.length,
        totalFrames: _performance.totalFrames,
        percentOver33ms: (_performance.stats.framesOver33ms / _performance.frameTimes.length) * 100
    };
}

/**
 * Resets performance statistics.
 */
export function resetPerformanceStats() {
    _performance.frameTimes = [];
    _performance.totalFrames = 0;
    _performance.stats = {
        avgFrameTime: 0,
        minFrameTime: 0,
        maxFrameTime: 0,
        fps: 0,
        framesOver33ms: 0
    };
}

// ---------------------------------------------------------------------------
// Dispose
// ---------------------------------------------------------------------------

/**
 * Cleans up all resources and event listeners.
 */
export function dispose() {
    _unbindEvents();

    if (_resizeObserver) {
        _resizeObserver.disconnect();
        _resizeObserver = null;
    }
    window.removeEventListener('resize', _fitCanvasToParent);

    _canvas = null;
    _ctx = null;
    _dotNetRef = null;
    _lastWorldState = null;
    _terrainTiles.background = null;
    _terrainTiles.dirt = null;
}
