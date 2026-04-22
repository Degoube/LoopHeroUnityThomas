using System;
using UnityEngine;

/// <summary>
/// Handles spawning of slashable objects in arc trajectories.
/// Manages spawn rate, difficulty progression, and object pooling area.
/// </summary>
public class RelicSlashSpawner : MonoBehaviour
{
    /// <summary>Fired when a new object is spawned. Used by the game to subscribe to slash events.</summary>
    public event Action<SlashableObject> OnObjectSpawned;

    [Header("Spawn Area")]
    [Tooltip("Left boundary for spawn X position.")]
    [SerializeField] private float spawnMinX = -6f;

    [Tooltip("Right boundary for spawn X position.")]
    [SerializeField] private float spawnMaxX = 6f;

    [Tooltip("Y position from which objects are launched.")]
    [SerializeField] private float spawnY = -4f;

    [Header("Object Template")]
    [Tooltip("Base prefab for slashable objects. Must have SlashableObject + Collider.")]
    [SerializeField] private GameObject slashableObjectPrefab;

    private RelicSlashConfig config;
    private Camera gameCamera;
    private float spawnTimer;
    private float currentSpawnInterval;
    private float elapsedTime;
    private int activeObjects;
    private bool isSpawning;

    /// <summary>
    /// Initializes the spawner with config and camera reference.
    /// </summary>
    public void Initialize(RelicSlashConfig slashConfig, Camera cam)
    {
        config = slashConfig;
        gameCamera = cam;
        currentSpawnInterval = config.initialSpawnInterval;
        spawnTimer = 0f;
        elapsedTime = 0f;
        activeObjects = 0;
        isSpawning = true;
    }

    /// <summary>
    /// Stops spawning new objects.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    private void Update()
    {
        if (!isSpawning || config == null) return;

        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // Increase difficulty over time
        currentSpawnInterval = Mathf.Max(
            config.minSpawnInterval,
            config.initialSpawnInterval - (config.spawnAcceleration * elapsedTime));

        if (spawnTimer >= currentSpawnInterval && activeObjects < config.maxObjectsOnScreen)
        {
            spawnTimer = 0f;
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        if (slashableObjectPrefab == null)
        {
            Debug.LogWarning("[RelicSlashSpawner] No slashable object prefab assigned.");
            return;
        }

        // Determine if trap
        bool isTrap = UnityEngine.Random.value < config.trapChance;

        // Get object data
        SlashableObjectData data;
        if (isTrap && config.trapData != null)
        {
            data = config.trapData;
        }
        else if (!isTrap && config.objectTypes != null && config.objectTypes.Length > 0)
        {
            data = config.objectTypes[UnityEngine.Random.Range(0, config.objectTypes.Length)];
        }
        else
        {
            data = new SlashableObjectData
            {
                objectName = isTrap ? "Piege" : "Relique",
                objectColor = isTrap ? Color.red : Color.yellow,
                scale = 1f,
                bonusPoints = 0
            };
        }

        // Spawn position
        float spawnX = UnityEngine.Random.Range(spawnMinX, spawnMaxX);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        // Launch velocity (arc upward with horizontal spread)
        float launchSpeed = UnityEngine.Random.Range(config.minLaunchSpeed, config.maxLaunchSpeed);
        float horizontalDir = UnityEngine.Random.Range(-config.horizontalSpread, config.horizontalSpread);

        // Bias horizontal direction toward center
        if (spawnX > 0) horizontalDir -= 1f;
        else horizontalDir += 1f;

        Vector3 launchVelocity = new Vector3(horizontalDir, launchSpeed, 0f);

        // Instantiate
        GameObject obj = Instantiate(slashableObjectPrefab, spawnPos, Quaternion.identity, transform);
        SlashableObject slashable = obj.GetComponent<SlashableObject>();

        if (slashable == null)
        {
            Debug.LogError("[RelicSlashSpawner] Prefab is missing SlashableObject component.");
            Destroy(obj);
            return;
        }

        slashable.Initialize(launchVelocity, config.gravity, config.destroyBelowY,
            isTrap, data.objectColor, data.scale, data.bonusPoints, gameCamera);

        // Track active count
        activeObjects++;
        slashable.OnSlashed += HandleObjectDestroyed;
        slashable.OnMissed += HandleObjectMissed;

        OnObjectSpawned?.Invoke(slashable);
    }

    private void HandleObjectDestroyed(SlashableObject obj, bool wasTrap)
    {
        activeObjects = Mathf.Max(0, activeObjects - 1);
        obj.OnSlashed -= HandleObjectDestroyed;
        obj.OnMissed -= HandleObjectMissed;
    }

    private void HandleObjectMissed(SlashableObject obj)
    {
        activeObjects = Mathf.Max(0, activeObjects - 1);
        obj.OnSlashed -= HandleObjectDestroyed;
        obj.OnMissed -= HandleObjectMissed;
    }
}
