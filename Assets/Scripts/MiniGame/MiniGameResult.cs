/// <summary>
/// Result passed from a mini-game back to MiniGameManager when it ends.
/// </summary>
public struct MiniGameResult
{
    /// <summary>True if the player succeeded at the mini-game.</summary>
    public bool Success;

    /// <summary>Optional resource delta to apply after the mini-game (positive = gain, negative = loss).</summary>
    public int ResourceDelta;

    /// <summary>Optional narrative flag to set on success.</summary>
    public string FlagToAdd;

    public static MiniGameResult Win(int resourceDelta = 0, string flagToAdd = null)
        => new MiniGameResult { Success = true,  ResourceDelta = resourceDelta, FlagToAdd = flagToAdd };

    public static MiniGameResult Lose(int resourceDelta = 0)
        => new MiniGameResult { Success = false, ResourceDelta = resourceDelta };
}
