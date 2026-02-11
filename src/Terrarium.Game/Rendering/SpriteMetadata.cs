using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrarium.Game.Rendering;

/// <summary>
/// Pixel dimensions for small and large sprite variants.
/// </summary>
public sealed record FrameSizeInfo(
    [property: JsonPropertyName("small")] int Small,
    [property: JsonPropertyName("large")] int Large);

/// <summary>
/// Width and height of a sprite sheet image in pixels.
/// </summary>
public sealed record SheetDimensions(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

/// <summary>
/// References to the BMP sprite sheet files for each size variant.
/// </summary>
public sealed record SpriteSheetRef(
    [property: JsonPropertyName("small")] string? Small,
    [property: JsonPropertyName("large")] string? Large);

/// <summary>
/// Sheet dimensions for each size variant.
/// </summary>
public sealed record SheetSizeInfo(
    [property: JsonPropertyName("small")] SheetDimensions? Small,
    [property: JsonPropertyName("large")] SheetDimensions? Large);

/// <summary>
/// A single animation sequence within a sprite sheet.
/// </summary>
public sealed record AnimationSequence(
    [property: JsonPropertyName("row")] int Row,
    [property: JsonPropertyName("startFrame")] int StartFrame,
    [property: JsonPropertyName("frameCount")] int FrameCount,
    [property: JsonPropertyName("frameDuration")] int FrameDuration,
    [property: JsonPropertyName("loop")] bool Loop);

/// <summary>
/// All animation data for a single creature type.
/// </summary>
public sealed record CreatureAnimations(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("spriteSheet")] SpriteSheetRef SpriteSheet,
    [property: JsonPropertyName("sheetSize")] SheetSizeInfo SheetSize,
    [property: JsonPropertyName("animations")] Dictionary<string, AnimationSequence> Animations);

/// <summary>
/// Root object for the animations.json metadata file.
/// </summary>
public sealed record SpriteAnimationData(
    [property: JsonPropertyName("frameSize")] FrameSizeInfo FrameSize,
    [property: JsonPropertyName("creatures")] Dictionary<string, CreatureAnimations> Creatures);

/// <summary>
/// Service that loads and queries sprite animation metadata from animations.json.
/// </summary>
public sealed class SpriteMetadata
{
    private readonly SpriteAnimationData _data;

    private SpriteMetadata(SpriteAnimationData data)
    {
        _data = data;
    }

    /// <summary>
    /// Loads sprite metadata from a JSON string.
    /// </summary>
    public static SpriteMetadata Load(string json)
    {
        var data = JsonSerializer.Deserialize<SpriteAnimationData>(json)
            ?? throw new InvalidOperationException("Failed to deserialize sprite animation data.");
        return new SpriteMetadata(data);
    }

    /// <summary>
    /// Loads sprite metadata from a file path.
    /// </summary>
    public static async Task<SpriteMetadata> LoadFromFileAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return Load(json);
    }

    /// <summary>
    /// Gets the root animation data.
    /// </summary>
    public SpriteAnimationData Data => _data;

    /// <summary>
    /// Gets all known creature family names.
    /// </summary>
    public IEnumerable<string> CreatureFamilies => _data.Creatures.Keys;

    /// <summary>
    /// Gets animation data for a creature family, or null if not found.
    /// </summary>
    public CreatureAnimations? GetCreature(string family)
    {
        return _data.Creatures.GetValueOrDefault(family.ToLowerInvariant());
    }

    /// <summary>
    /// Gets a specific animation sequence for a creature family and action.
    /// </summary>
    /// <param name="family">Creature family name (e.g., "ant", "spider").</param>
    /// <param name="action">Animation action key (e.g., "moved_n", "attacked_se", "idle").</param>
    /// <returns>The animation sequence, or null if not found.</returns>
    public AnimationSequence? GetAnimation(string family, string action)
    {
        var creature = GetCreature(family);
        return creature?.Animations.GetValueOrDefault(action.ToLowerInvariant());
    }

    /// <summary>
    /// Gets frame pixel coordinates for a specific frame within an animation.
    /// </summary>
    /// <param name="family">Creature family name.</param>
    /// <param name="action">Animation action key.</param>
    /// <param name="frameIndex">Zero-based frame index within the animation.</param>
    /// <param name="useLarge">True to use 48px frames, false for 24px.</param>
    /// <returns>Tuple of (x, y, width, height) in pixels, or null if not found.</returns>
    public (int X, int Y, int Width, int Height)? GetFrameRect(
        string family, string action, int frameIndex, bool useLarge = true)
    {
        var animation = GetAnimation(family, action);
        if (animation is null)
            return null;

        var frameSize = useLarge ? _data.FrameSize.Large : _data.FrameSize.Small;
        var frame = animation.StartFrame + (frameIndex % animation.FrameCount);

        return (frame * frameSize, animation.Row * frameSize, frameSize, frameSize);
    }
}
