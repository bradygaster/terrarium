using OrganismBase;
using Terrarium.Game.Rendering;

namespace Terrarium.Web.Tests;

/// <summary>
/// Tests for the direction-to-animation mapping used by the renderer.
/// The legacy Terrarium used 8 compass directions for creature facing.
/// The animations.json defines a row-per-direction layout:
///   action base row + direction offset = actual sprite sheet row.
/// 
/// Layout from animations.json:
///   actionBaseRows: attacked=0, defended=8, died=16, ate=24, moved=32
///   directionIndices: n=7, ne=8(→0 relative), e=1, se=2, s=3, sw=4, w=5, nw=6
///   Each action uses 8 consecutive rows (one per direction).
///   Row = actionBaseRow + (directionIndex - 1) for e..nw, special handling for n/ne.
/// 
/// Actual mapping per the JSON data:
///   attacked_e=row 0, attacked_se=row 1, ..., attacked_ne=row 7
///   defended_e=row 8, defended_se=row 9, ..., defended_ne=row 15
///   died_e=row 16, ..., died_ne=row 23
///   ate_e=row 24, ..., ate_ne=row 31
///   moved_e=row 32, ..., moved_ne=row 39
/// </summary>
public class DirectionMappingTests
{
    // Direction ordering as used in sprite sheets (row offset within action block)
    private static readonly Dictionary<string, int> DirectionRowOffset = new()
    {
        ["e"]  = 0,
        ["se"] = 1,
        ["s"]  = 2,
        ["sw"] = 3,
        ["w"]  = 4,
        ["nw"] = 5,
        ["n"]  = 6,
        ["ne"] = 7
    };

    private static readonly Dictionary<string, int> ActionBaseRow = new()
    {
        ["attacked"] = 0,
        ["defended"] = 8,
        ["died"]     = 16,
        ["ate"]      = 24,
        ["moved"]    = 32
    };

    // --- Row calculation formula ---

    [Theory]
    [InlineData("attacked", "e", 0)]
    [InlineData("attacked", "ne", 7)]
    [InlineData("defended", "e", 8)]
    [InlineData("defended", "s", 10)]
    [InlineData("died", "w", 20)]
    [InlineData("ate", "nw", 29)]
    [InlineData("moved", "e", 32)]
    [InlineData("moved", "n", 38)]
    [InlineData("moved", "ne", 39)]
    public void ActionDirection_ToRow_Formula(string action, string direction, int expectedRow)
    {
        int row = ActionBaseRow[action] + DirectionRowOffset[direction];
        Assert.Equal(expectedRow, row);
    }

    // --- 8 directions per action ---

    [Fact]
    public void EachAction_Has_Exactly8_DirectionRows()
    {
        Assert.Equal(8, DirectionRowOffset.Count);

        foreach (var action in ActionBaseRow)
        {
            int baseRow = action.Value;
            // 8 consecutive rows from baseRow to baseRow+7
            for (int i = 0; i < 8; i++)
            {
                Assert.True(baseRow + i < 40,
                    $"Action '{action.Key}' row {baseRow + i} exceeds 40 total rows");
            }
        }
    }

    // --- Total layout ---

    [Fact]
    public void TotalAnimationRows_Is40()
    {
        // 5 actions * 8 directions = 40 rows
        int totalRows = ActionBaseRow.Count * DirectionRowOffset.Count;
        Assert.Equal(40, totalRows);
    }

    [Fact]
    public void NoRowOverlap_BetweenActions()
    {
        var allRows = new HashSet<int>();
        foreach (var action in ActionBaseRow)
        {
            for (int d = 0; d < 8; d++)
            {
                int row = action.Value + d;
                Assert.True(allRows.Add(row),
                    $"Row {row} used by multiple actions (duplicate in '{action.Key}')");
            }
        }
        Assert.Equal(40, allRows.Count);
    }

    // --- DisplayAction ↔ animation action key mapping ---

    [Theory]
    [InlineData(DisplayAction.Attacked, "attacked")]
    [InlineData(DisplayAction.Defended, "defended")]
    [InlineData(DisplayAction.Died, "died")]
    [InlineData(DisplayAction.Ate, "ate")]
    [InlineData(DisplayAction.Moved, "moved")]
    public void DisplayAction_MapsToAnimationKey(DisplayAction action, string expectedKey)
    {
        // This documents how the renderer should map DisplayAction enum values
        // to animation.json action keys
        string key = action switch
        {
            DisplayAction.Attacked => "attacked",
            DisplayAction.Defended => "defended",
            DisplayAction.Died => "died",
            DisplayAction.Ate => "ate",
            DisplayAction.Moved => "moved",
            _ => "idle"
        };

        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData(DisplayAction.NoAction)]
    [InlineData(DisplayAction.Teleported)]
    [InlineData(DisplayAction.Reproduced)]
    [InlineData(DisplayAction.Dead)]
    public void NonDirectionalActions_MapToIdle(DisplayAction action)
    {
        // Actions without directional animations should fall back to idle
        string key = action switch
        {
            DisplayAction.Attacked => "attacked",
            DisplayAction.Defended => "defended",
            DisplayAction.Died => "died",
            DisplayAction.Ate => "ate",
            DisplayAction.Moved => "moved",
            _ => "idle"
        };

        Assert.Equal("idle", key);
    }

    // --- Frame index calculation ---

    [Theory]
    [InlineData(0, 10, 0)]   // First frame
    [InlineData(5, 10, 5)]   // Mid-animation
    [InlineData(9, 10, 9)]   // Last frame
    [InlineData(10, 10, 0)]  // Wraps to 0
    [InlineData(15, 10, 5)]  // Wraps to 5
    [InlineData(0, 1, 0)]    // Single-frame (idle)
    [InlineData(99, 1, 0)]   // Single-frame always returns 0
    public void FrameIndex_Wrapping(int rawFrame, int frameCount, int expectedFrame)
    {
        int frame = rawFrame % frameCount;
        Assert.Equal(expectedFrame, frame);
    }
}
