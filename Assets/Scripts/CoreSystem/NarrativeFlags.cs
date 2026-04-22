/// <summary>
/// Centralized narrative flag constants. Use these instead of raw strings
/// to avoid typos and ease maintenance.
/// </summary>
public static class NarrativeFlags
{
    // Game lifecycle
    public const string GameStarted    = "game_started";
    public const string GameWon        = "game_won";
    public const string GameLost       = "game_lost";

    // Quest milestones
    public const string VisitedRuins   = "visited_ruins";
    public const string ActivatedAltar = "activated_altar";
    public const string FoundRelic     = "found_relic";

    // Witness narrative arc
    public const string MetWitness     = "met_witness";
    public const string LoopAware      = "loop_aware";
    public const string TruthDone      = "truth_done";

    // Mini-games
    public const string RuinsMiniGameWon  = "ruins_minigame_won";
    public const string CombatMiniGameWon = "combat_minigame_won";
    public const string RelicMiniGameWon  = "relic_minigame_won";
    public const string AltarMiniGameWon  = "altar_minigame_won";

    // Victory flag (must match GameStateManager.victoryFlag default)
    public const string Victory        = TruthDone;
}
