using System;

/// <summary>
/// Contract that every mini-game MonoBehaviour must implement.
/// MiniGameManager drives the lifecycle; the mini-game fires OnMiniGameEnded when done.
/// </summary>
public interface IMiniGame
{
    /// <summary>Fired by the mini-game when the player finishes (win or lose).</summary>
    event Action<MiniGameResult> OnMiniGameEnded;

    /// <summary>Called by MiniGameManager to start the mini-game with optional context data.</summary>
    void StartMiniGame(BoardTile sourceTile);
}
