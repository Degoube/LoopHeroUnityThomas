/// <summary>
/// Result passed from a mini-game back to MiniGameManager when it ends.
/// </summary>
public struct MiniGameResult
{
    /// <summary>True if the player succeeded at the mini-game.</summary>
    public bool Success;

    /// <summary>Optional resource delta to apply after the mini-game (positive = gain, negative = loss).</summary>
    public int ResourceDelta;

    /// <summary>XP delta to apply after the mini-game (always positive or zero).</summary>
    public int XPDelta;

    /// <summary>Optional narrative flag to set on success.</summary>
    public string FlagToAdd;

    public static MiniGameResult Win(int resourceDelta = 0, int xpDelta = 0, string flagToAdd = null)
        => new MiniGameResult { Success = true, ResourceDelta = resourceDelta, XPDelta = xpDelta, FlagToAdd = flagToAdd };

    public static MiniGameResult Lose(int resourceDelta = 0, int xpDelta = 0)
        => new MiniGameResult { Success = false, ResourceDelta = resourceDelta, XPDelta = xpDelta };
}
