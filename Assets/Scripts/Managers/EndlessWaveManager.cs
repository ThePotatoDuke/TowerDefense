using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public class EndlessWaveManager : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Wave Configuration")]
    [SerializeField] private List<Wave> waves;

    [Header("Sky Drops")]
    [Tooltip("Where along the path (0 = start, 1 = castle) enemies that drop from the sky can land; " +
             "picked at random between the two.")]
    [SerializeField] private Vector2 skyDropRange = new Vector2(0.1f, 0.5f);
    [Tooltip("How far out a roller's end markers sit, as a fraction of half its length (1 = right at its ends).")]
    [SerializeField, Range(0f, 1f)] private float rollerMarkerSpread = 0.75f;

    [Header("Scaling")]
    [SerializeField] private int enemiesPerWaveIncrement = 2;

    [Header("Starting Weapons")]
    [Tooltip("Weapon pickups whose weapons the player starts the run carrying, in slot order.")]
    [SerializeField] private List<GameObject> weaponPrefabs;

    private int totalWaveCount = 0;
    // The wave the run is on (1 for the first); the last one reached once the run is over
    public int CurrentWave => totalWaveCount;
    private int waveLoopIndex = 0;

    private bool isSpawning;
    private bool spawnRemainingInstantly;

    // Events
    public event Action<int> OnWaveChanged;
    public event Action<EnemyBase> OnEnemySpawned;

    private void OnEnable()
    {
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        GiveStartingWeapons();
        StartCoroutine(RunEndlessWaves());
    }

    private void GiveStartingWeapons()
    {
        WeaponInventory inventory = Player.Instance != null ? Player.Instance.Inventory : null;
        if (inventory == null) return;

        foreach (GameObject prefab in weaponPrefabs)
        {
            WeaponPickup pickup = prefab != null ? prefab.GetComponent<WeaponPickup>() : null;
            if (pickup != null)
                inventory.Add(pickup.weaponSO);
        }
        inventory.Select(0); // the first one in hand
    }

    private void Update()
    {
        // Debug: spawn the rest of the current wave immediately (F, as E plants Crimson Weed)
        if (Input.GetKeyDown(KeyCode.F) && isSpawning)
            spawnRemainingInstantly = true;
    }

    private void HandleGameStateChanged(GameState state)
    {
        // Stop spawning once the run is over
        if (state == GameState.CastleDestroyed || state == GameState.PlayerDead)
            StopAllCoroutines();
    }

    private IEnumerator RunEndlessWaves()
    {
        while (true)
        {
            foreach (var wave in waves)
            {
                totalWaveCount++;
                OnWaveChanged?.Invoke(totalWaveCount);

                // Spawn enemies
                yield return SpawnWave(wave);

                // Wait until all wave-blocking enemies are dead (spike balls don't count)
                yield return new WaitUntil(() => EnemyManager.Instance.GetWaveBlockingEnemyCount() == 0);

                GameEvents.WaveCleared();

                // Every few waves, the shop (the game waits while it's open)
                ShopSettings shopSettings = ShopSettings.Instance;
                Shop shop = Shop.Instance;
                if (shop != null && shopSettings != null && shopSettings.wavesBetweenShops > 0
                    && totalWaveCount % shopSettings.wavesBetweenShops == 0)
                {
                    yield return new WaitForSeconds(shopSettings.openDelay);
                    yield return shop.Visit();
                }
            }

            waveLoopIndex++;
        }
    }


    private IEnumerator SpawnWave(Wave wave)
    {
        // Build weighted list for extra/random enemies
        // Entries, not just prefabs, so per-entry settings (e.g. ranged immunity) come along
        List<EnemySpawnEntry> weightedList = new List<EnemySpawnEntry>();
        foreach (var entry in wave.enemies)
        {
            int reps = Mathf.CeilToInt(entry.spawnWeight * 10);
            for (int i = 0; i < reps; i++)
                weightedList.Add(entry);
        }

        // Build combined spawn list
        List<EnemySpawnEntry> spawnList = new List<EnemySpawnEntry>();

        // Add guaranteed base counts
        foreach (var entry in wave.enemies)
            for (int i = 0; i < entry.baseCount; i++)
                spawnList.Add(entry);

        // Add extra enemies (random amount)
        int minExtra = 1;
        int maxExtra = Mathf.Max(minExtra, waveLoopIndex * enemiesPerWaveIncrement + minExtra);
        int extraEnemies = UnityEngine.Random.Range(minExtra, maxExtra + 1);

        // Skip extras if every entry has spawnWeight 0
        for (int i = 0; i < extraEnemies && weightedList.Count > 0; i++)
        {
            spawnList.Add(weightedList[UnityEngine.Random.Range(0, weightedList.Count)]);
        }

        // Shuffle spawn list
        for (int i = spawnList.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = spawnList[i];
            spawnList[i] = spawnList[j];
            spawnList[j] = temp;
        }

        // Spawn enemies
        isSpawning = true;
        spawnRemainingInstantly = false;
        foreach (var enemy in spawnList)
        {
            SpawnEnemy(enemy);
            if (!spawnRemainingInstantly)
                yield return new WaitForSeconds(RandomInterval(wave));
        }
        isSpawning = false;
    }

    private void SpawnEnemy(EnemySpawnEntry entry)
    {
        EnemyBase instance = Instantiate(entry.enemyPrefab);
        instance.AssignSpline(splineContainer);
        if (entry.immuneToRanged)
            instance.SetImmuneToRanged(true);

        if (splineContainer != null && instance.Data != null && instance.Data.dropsFromSky)
        {
            // Off until it lands on a marked spot along the path
            float progress = UnityEngine.Random.Range(skyDropRange.x, skyDropRange.y);
            instance.PlaceOnPath(progress);
            instance.gameObject.SetActive(false);
            SkyDrop.Begin(instance, instance.transform.position, DropMarkers(instance, progress));
        }
        else
        {
            if (splineContainer != null)
                instance.transform.position = splineContainer.EvaluatePosition(splineContainer.Spline, 0f);
            PopIn(instance.transform);
        }

        instance.SetHealthMultiplier(1f);
        OnEnemySpawned?.Invoke(instance);

        // Unregister on death
        instance.OnDied += () => EnemyManager.Instance.UnregisterEnemy(instance);
    }

    // Where to mark a sky drop, relative to the spot: one in the middle, or for a roller three in a line
    // across the path, where its length will lie (middle and near each end)
    private Vector3[] DropMarkers(EnemyBase enemy, float progress)
    {
        if (enemy is not RollerEnemy roller || roller.HalfLength <= 0f) return null;

        Vector3 along = splineContainer.EvaluateTangent(progress);
        along.y = 0f;
        if (along.sqrMagnitude < 0.0001f) return null;
        Vector3 across = Vector3.Cross(Vector3.up, along.normalized);

        float reach = roller.HalfLength * rollerMarkerSpread;
        return new[] { -across * reach, Vector3.zero, across * reach };
    }

    // Scales up from nothing as it appears
    private static void PopIn(Transform enemy)
    {
        float time = FXSettings.Instance != null ? FXSettings.Instance.spawnPopTime : 0.3f;
        if (time <= 0f) return;

        Vector3 fullSize = enemy.localScale;
        enemy.localScale = Vector3.zero;
        enemy.DOScale(fullSize, time).SetEase(Ease.OutBack).SetLink(enemy.gameObject);
    }

    private float RandomInterval(Wave wave)
    {
        float interval = UnityEngine.Random.Range(wave.spawnIntervalMin, wave.spawnIntervalMax);
        if (UnityEngine.Random.value < wave.burstChance)
            interval *= 0.5f;
        return interval;
    }
}

[System.Serializable]
public class Wave
{
    public List<EnemySpawnEntry> enemies;
    public float spawnIntervalMin = 0.3f;
    public float spawnIntervalMax = 0.7f;
    [Range(0f, 1f)]
    public float burstChance = 0.2f;
}

[System.Serializable]
public class EnemySpawnEntry
{
    public EnemyBase enemyPrefab;
    [Range(0f, 1f)]
    public float spawnWeight = 1f;
    public int baseCount = 1;
    [Tooltip("Enemies spawned from this entry are immune to ranged attacks, whatever their prefab says.")]
    public bool immuneToRanged;
}
