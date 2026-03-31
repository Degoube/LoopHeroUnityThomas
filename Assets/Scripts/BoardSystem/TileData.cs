using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Data", menuName = "Board/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Tile Identity")]
    public TileType tileType;
    public string tileName;

    [TextArea(2, 4)]
    public string description;

    [Header("Visual")]
    public Sprite tileIcon;
    public Color tileColor = Color.white;
    public GameObject tilePrefab;

    [Header("Gameplay")]
    public int encounterChance = 0;
    public bool isPassable = true;
    public bool triggersOnEnter = true;
    public bool canBeRevisited = true;

    [Header("Mini-Game")]
    [Tooltip("If true, landing on this tile launches the assigned mini-game prefab.")]
    public bool triggersMiniGame = false;
    [Tooltip("Prefab must have a component that implements IMiniGame.")]
    public GameObject miniGamePrefab;

    [Header("Rewards")]
    public int goldReward;
    public int experienceReward;

    [Header("Audio")]
    public AudioClip enterSound;
    public AudioClip actionSound;
}
